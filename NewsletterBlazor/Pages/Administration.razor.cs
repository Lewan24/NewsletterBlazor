using Microsoft.AspNetCore.Identity;
using NewsletterBlazor.Data.Entities;

namespace NewsletterBlazor.Pages;

#nullable disable warnings

partial class Administration
{
    Pagination pagination = new(15);
    
    IdentityUser SelectedUser = new();
    List<IdentityUser> UsersList;

    bool ShowPopup = false;

    string CurrentUserRole { get; set; } = "User";
    string? Filter;
    string strError = "";
    string success = "";

    List<string> Options = new() { "User", "Admin"};

    protected async override Task OnInitializedAsync()
    {
        GetUsers();

        var authState = await _AuthenticationStateProvider.GetAuthenticationStateAsync();
        var userPrincipal = authState.User.Identity;
        var user = UsersList.FirstOrDefault(u => u.UserName == userPrincipal.Name);

        var roles = new List<string>() { "Admin", "User" };
        
        if (user.Email == _config["Administration:Admin"] & !await _UserManager.IsInRoleAsync(user, "Admin"))
            await _UserManager.AddToRolesAsync(user, roles);

        await pagination.LoadSettings(UsersList);
    }

    #region Roles management
    async Task AddRole()
    {
        success = "";
        strError = "";

        var user = await _UserManager.FindByIdAsync(SelectedUser.Id);

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
        success = "";
        strError = "";

        var user = await _UserManager.FindByIdAsync(SelectedUser.Id);

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
        success = "";
        strError = "";

        var user = await _UserManager.FindByIdAsync(SelectedUser.Id);

        var userRoles = await _UserManager.GetRolesAsync(user);

        success = "User Roles: ";

        foreach (var role in userRoles)
            success += $"{role}, ";
    }
    #endregion

    #region User Management
    async Task EditUser(IdentityUser _IdentityUser)
    {
        SelectedUser = _IdentityUser;
        var user = await _UserManager.FindByIdAsync(SelectedUser.Id);

        if (user is null)
            return;

        var UserResult = await _UserManager.IsInRoleAsync(user, "Admin");

        if (UserResult)
            CurrentUserRole = "Admin";
        else
            CurrentUserRole = "User";

        ShowPopup = true;
    }
    async Task SaveUser()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(SelectedUser.Id))
            {
                var user = await _UserManager.FindByIdAsync(SelectedUser.Id);
                user.Email = SelectedUser.Email;
                await _UserManager.UpdateAsync(user);

                if (SelectedUser.PasswordHash != "******")
                {
                    var resetToken = await _UserManager.GeneratePasswordResetTokenAsync(user);
                    var passwordUser = await _UserManager.ResetPasswordAsync(user, resetToken, SelectedUser.PasswordHash);

                    if (!passwordUser.Succeeded)
                    {
                        if (passwordUser.Errors.FirstOrDefault() != null)
                        {
                            strError = passwordUser.Errors.FirstOrDefault().Description;
                            return;
                        }

                        strError = "Password error";
                        return;
                    }
                }

                var UserResult = await _UserManager.IsInRoleAsync(user, "Admin");

                if ((CurrentUserRole == "Admin") & (!UserResult))
                    await _UserManager.AddToRoleAsync(user, "Admin");

                if ((CurrentUserRole != "Admin") & (UserResult))
                    await _UserManager.RemoveFromRoleAsync(user, "Admin");
            }
            else
            {
                var NewUser = new IdentityUser { UserName = SelectedUser.UserName, Email = SelectedUser.Email };
                var CreateResult = await _UserManager.CreateAsync(NewUser, SelectedUser.PasswordHash);

                if (!CreateResult.Succeeded)
                {
                    if (CreateResult.Errors.FirstOrDefault() != null)
                    {
                        strError = CreateResult.Errors.FirstOrDefault().Description;
                        return;
                    }

                    strError = "Create error";
                    return;
                }

                if (CreateResult.Succeeded)
                    if (!await _UserManager.IsInRoleAsync(NewUser, "User"))
                        await _UserManager.AddToRoleAsync(NewUser, "User");
            }

            ShowPopup = false;
            GetUsers();

            await pagination.LoadSettings(UsersList);
        }
        catch (Exception ex)
        {
            strError = ex.GetBaseException().Message;
        }
    }
    async Task DeleteUser()
    {
        ShowPopup = false;

        var user = await _UserManager.FindByIdAsync(SelectedUser.Id);

        if (user != null)
            await _UserManager.DeleteAsync(user);

        GetUsers();

        await pagination.LoadSettings(UsersList);
    }
    #endregion

    private bool IsVisible(IdentityUser user)
    {
        if (string.IsNullOrEmpty(Filter))
            return true;

        if (user.Email.Contains(Filter, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    async Task ConfirmEmail(string id)
    {
        var user = await _UserManager.FindByIdAsync(id);

        if (user is null)
            return;

        var token = await _UserManager.GenerateEmailConfirmationTokenAsync(user);
        await _UserManager.ConfirmEmailAsync(user, token);

        GetUsers();

        await pagination.LoadSettings(UsersList);
    }

    public void GetUsers()
    {
        strError = "";

        UsersList = new();

        var usersFromDB = _UserManager.Users.Select(x => new IdentityUser
        {
            Id = x.Id,
            UserName = x.UserName,
            Email = x.Email,
            EmailConfirmed = x.EmailConfirmed,
            PasswordHash = "******"
        });

        foreach (var user in usersFromDB)
            UsersList.Add(user);
    }

    void ClosePopup() => ShowPopup = false;
}
