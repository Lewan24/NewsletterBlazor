using System.Security.Policy;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using MudBlazor;
using TextCopy;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class Administration
{
    public List<string> Options = new() { "User", "Admin" };
    private List<IdentityUser> _usersList = new();

    private string CurrentUserRole { get; set; } = "User";
    private string _searchEMail;

    private bool _loadingUsers = true;
    private bool success;

    private IdentityUser _selectedUser = new();
    private IdentityUser _newUser = new();

    private MudForm form;

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
    async Task AddRole()
    {
        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        var doesUserHasRole = await _UserManager.IsInRoleAsync(user, CurrentUserRole);

        if (!doesUserHasRole)
        {
            await _UserManager.AddToRoleAsync(user, CurrentUserRole);
            success = $"Successfully added {CurrentUserRole} role to user";
            return;
        }

        strError = "User is already in this role.";
    }
    async Task RemoveRole()
    {
        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        var doesUserHasRole = await _UserManager.IsInRoleAsync(user, CurrentUserRole);

        if (!doesUserHasRole)
        {
            strError = "User does not has this role.";
            return;
        }

        await _UserManager.RemoveFromRoleAsync(user, CurrentUserRole);
        success = $"Successfully removed {CurrentUserRole} role from user";
    }
    async Task CheckRoles()
    {
        var user = await _UserManager.FindByIdAsync(_selectedUser.Id);

        var userRoles = await _UserManager.GetRolesAsync(user);

        success = "User Roles: ";

        foreach (var role in userRoles)
            success += $"{role}, ";
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
            var NewUser = new IdentityUser { UserName = _selectedUser.UserName, Email = _selectedUser.Email };
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
        bool? result = await DialogService.ShowMessageBox(
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
