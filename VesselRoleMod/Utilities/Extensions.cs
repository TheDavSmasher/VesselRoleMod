namespace VesselRoleMod.Utilities;

public static class Extensions
{
	public static bool HasKillingAbility(this PlayerControl player)
	{
		return false;
	}

	public static bool HasKillingAbility(this RoleBehaviour role)
	{
		return false;
	}
}
