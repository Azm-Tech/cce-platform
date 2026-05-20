namespace CCE.Infrastructure.Identity;

public static class AdRoleMapper
{
    public static string? ToCceRole(string adGroup)
    {
        return adGroup switch
        {
            "CCE-SuperAdmins" => "cce-super-admin",
            "CCE-Admins" => "cce-admin",
            "CCE-ContentManagers" => "cce-content-manager",
            "CCE-StateRepresentatives" => "cce-state-representative",
            "CCE-Reviewers" => "cce-reviewer",
            "CCE-Experts" => "cce-expert",
            "CCE-Users" => "cce-user",
            _ => null,
        };
    }
}
