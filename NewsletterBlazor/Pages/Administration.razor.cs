using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using MudBlazor;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class Administration
{
    private List<IdentityUser> _usersList = new();
    
    private string _searchEMail;
    private List<string> _currentUserRoles = new();

    private bool _loadingUsers = true;
    private bool success;

    private IdentityUser _selectedUser = new();
    private IdentityUser _newUser = new();
    
    private MudForm form;

    // TODO: Add entity with list of roles, then apply that list to every selection etc
    private List<string> AllRoles = new() { "User", "Admin" };

    protected override async Task OnInitializedAsync()
    {
        GetUsers();

        var authState = await _AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userPrincipal = authState.User.Identity;
        var user = _usersList.FirstOrDefault(u => u.UserName == userPrincipal.Name);

        var roles = new List<string> { "Admin", "User" };
        
        if (user.Email == _config["Administration:Admin"] && !await _UserManager.IsInRoleAsync(user, "Admin"))
            await _UserManager.AddToRolesAsync(user, roles);
    }
    private bool FilterFunc1(IdentityUser user) => FilterFunc(user, _searchEMail);
    private bool FilterFunc(IdentityUser user, string searchString)
    {
        if (string.IsNullOrWhiteSpace(searchString))
            return true;
        if (user.Email.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            return true;
        if (user.Id.Contains(searchString, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }
    private async Task CopyIdToClipboard<T>(T element)
    {
        if (element is null)
            return;

        await Clipboard.SetTextAsync(element.ToString());
        Snackbar.Add("Successfully copied selected element", Severity.Success);
    }
    private void GenerateNewGuid()
    {
        _newUser.Id = Guid.NewGuid().ToString();
        StateHasChanged();
    }
    private void ResetForm()
    {
        _selectedUser = new();
        _newUser = new();

        StateHasChanged();
    }
    private IEnumerable<string> PasswordStrength(string pw)
    {
        if (string.IsNullOrWhiteSpace(pw))
        {
            yield return "Password is required!";
            yield break;
        }
        if (pw.Length < 6)
            yield return "Password must be at least of length 6";
        if (!Regex.IsMatch(pw, @"[A-Z]"))
            yield return "Password must contain at least one capital letter";
        if (!Regex.IsMatch(pw, @"[a-z]"))
            yield return "Password must contain at least one lowercase letter";
        if (!Regex.IsMatch(pw, @"[0-9]"))
            yield return "Password must contain at least one digit";
    }

    #region Roles management
    async Task AddRole(string role)
    {
        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        var doesUserHasRole = await _UserManager.IsInRoleAsync(user, role);

        if (!doesUserHasRole)
        {
            await _UserManager.AddToRoleAsync(user, role);
            Snackbar.Add($"Successfully added {role} role to user", Severity.Success);
            return;
        }

        Snackbar.Add("User is already in this role.", Severity.Warning);

        StateHasChanged();
    }
    async Task RemoveRole(string role)
    {
        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        var doesUserHasRole = await _UserManager.IsInRoleAsync(user, role);

        if (!doesUserHasRole)
        {
            Snackbar.Add("User does not has this role.", Severity.Warning);
            return;
        }

        await _UserManager.RemoveFromRoleAsync(user, role);
        Snackbar.Add($"Successfully removed {role} role from user", Severity.Success);

        StateHasChanged();
    }
    async Task CheckRoles()
    {
        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        _currentUserRoles = (await _UserManager.GetRolesAsync(user)).ToList();

        StateHasChanged();
    }

    private bool CheckAvailableRoles()
    {
        var availableRoles = AllRoles.Count(role => _currentUserRoles.Contains(role));

        if (availableRoles > 0)
            return true;

        return false;
    }
    #endregion

    #region User Management
    async Task SaveUser()
    {
        try
        {
            var user = await _UserManager.FindByIdAsync(_selectedUser.Id);
            user.Email = _selectedUser.Email;
            user.UserName = _selectedUser.Email;
            await _UserManager.UpdateAsync(user);

            if (_selectedUser.PasswordHash != "******")
            {
                var resetToken = await _UserManager.GeneratePasswordResetTokenAsync(user);
                var passwordUser = await _UserManager.ResetPasswordAsync(user, resetToken, _selectedUser.PasswordHash);

                if (!passwordUser.Succeeded)
                {
                    if (passwordUser.Errors.FirstOrDefault() != null)
                    {
                        Snackbar.Add(passwordUser.Errors.FirstOrDefault().Description, Severity.Error);
                        return;
                    }

                    Snackbar.Add("Password error", Severity.Error);
                    return;
                }
            }

            GetUsers();
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
        }
    }
    async Task CreateUser()
    {
        try
        {
            var NewUser = new IdentityUser();

            if (_newUser.Id is null or "")
                NewUser = new IdentityUser { UserName = _selectedUser.UserName, Email = _selectedUser.Email };
            else
                NewUser = new IdentityUser { Id = _newUser.Id, UserName = _selectedUser.UserName, Email = _selectedUser.Email };

            var CreateResult = await _UserManager.CreateAsync(NewUser, _selectedUser.PasswordHash);

            if (!CreateResult.Succeeded)
            {
                if (CreateResult.Errors.FirstOrDefault() != null)
                {
                    Snackbar.Add(CreateResult.Errors.FirstOrDefault().Description, Severity.Error);
                    return;
                }

                Snackbar.Add("Create error", Severity.Error);
                return;
            }

            if (CreateResult.Succeeded)
                await _UserManager.AddToRoleAsync(NewUser, "User");

            GetUsers();
        }
        catch (Exception ex)
        {
            Snackbar.Add(ex.GetBaseException().Message, Severity.Error);
        }
    }
    async Task DeleteUser()
    {
        bool? result = await _dialogService.ShowMessageBox(
            "Warning",
            "Deleting can not be undone!",
            yesText: "Delete!", cancelText: "Cancel");

        if (result is null or false)
            return;

        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        if (user != null)
            await _UserManager.DeleteAsync(user);

        GetUsers();
    }
    #endregion

    async Task ConfirmEmail(string id)
    {
        var user = await _UserManager.FindByIdAsync(id);

        if (user is null)
            return;

        var token = await _UserManager.GenerateEmailConfirmationTokenAsync(user);
        await _UserManager.ConfirmEmailAsync(user, token);

        _usersList.FirstOrDefault(u => u.Id == id)!.EmailConfirmed = true;
        StateHasChanged();
    }
    private void GetUsers()
    {
        _loadingUsers = true;

        var usersFromDB = _UserManager.Users.Select(x => new IdentityUser
        {
            Id = x.Id,
            UserName = x.UserName,
            Email = x.Email,
            EmailConfirmed = x.EmailConfirmed,
            PasswordHash = "******"
        });

        foreach (var user in usersFromDB)
        {
            _usersList.Add(user);
            StateHasChanged();
        }

        _loadingUsers = false;
    }
}
