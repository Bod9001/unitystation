
/// <summary>
/// Main API for working with cooldowns. Can also use the HasCooldowns component on a particular object, but
/// this API is generally more succint.
/// </summary>
public static class Cooldowns
{
	/// <summary>
	/// Starts the cooldown for the player if it's not currently on.
	/// </summary>
	/// <param name="player"></param>
	/// <param name="cooldown">cooldown to try starting</param>
	/// <param name="side">indicates which side's cooldown should be started</param>
	/// <param name="secondsOverride">custom cooldown time in seconds</param>
	/// <returns>true if cooldown was successfully started, false if cooldown was already on.</returns>
	public static bool TryStart(Mind player, ICooldown cooldown, NetworkSide side, float secondsOverride=float.NaN)
	{
		return player.Cooldown.TryStart(cooldown, side, secondsOverride);
	}

	/// <summary>
	/// Starts the cooldown for the interaction's performer if it's not currently on.
	/// </summary>
	/// <param name="interaction">interaction whose performer's cooldown should be started</param>
	/// <param name="cooldown">cooldown to try starting</param>
	/// <param name="side">indicates which side's cooldown should be started</param>
	/// <returns>true if cooldown was successfully started, false if cooldown was already on.</returns>
	public static bool TryStart(Interaction interaction, ICooldown cooldown, NetworkSide side)
	{
		return TryStart(interaction.Performer, cooldown, side);
	}

	/// <summary>
	/// Same as TryStart with NetworkSide.Client
	/// </summary>
	public static bool TryStartClient(Mind player, ICooldown cooldown, float secondsOverride=float.NaN)
	{
		return TryStart(player, cooldown, NetworkSide.Client, secondsOverride);
	}


	/// <summary>
	/// Same as TryStart with NetworkSide.Client
	/// </summary>
	public static bool TryStartClient(Interaction interaction, ICooldown cooldown, float secondsOverride=float.NaN)
	{
		return TryStartClient(interaction.Performer, cooldown, secondsOverride);
	}

	/// <summary>
	/// Same as TryStart with NetworkSide.Server
	/// </summary>
	public static bool TryStartServer(Mind player, ICooldown cooldown, float secondsOverride=float.NaN)
	{
		return TryStart(player, cooldown, NetworkSide.Server, secondsOverride);
	}

	/// <summary>
	/// Same as TryStart with NetworkSide.Server
	/// </summary>
	public static bool TryStartServer(Interaction interaction, ICooldown cooldown, float secondsOverride=float.NaN)
	{
		return TryStartServer(interaction.Performer, cooldown, secondsOverride);
	}

	/// <summary>
	/// Starts a cooldown (if it's not already on) for the interaction's performer
	/// which is identified by a particular TYPE of interactable component. The specific instance
	/// doesn't matter - this cooldown is shared by all instances of that type. For example, you'd always start
	/// the same cooldown regardless of which object's Meleeable component you passed to this (this is
	/// usually the intended behavior for interactable components - you don't care which object's interaction
	/// you are triggering - you should still have the same cooldown for melee regardless of who you are hitting).
	/// Intended for convenience / one-off usage in small interactable components so you don't need
	/// to create an asset.
	/// </summary>
	/// <param name="cooldown">Should almost always be "this" (when called from within
	/// an interactable component). Interactable component whose cooldown should be started</param>
	/// <param name="seconds">how many seconds the cooldown should take</param>
	/// <param name="side">indicates which side's cooldown should be started</param>
	/// <returns>true if cooldown was successfully started, false if cooldown was already on.</returns>
	public static bool TryStart<T>(T interaction, IInteractable<T> interactable, float seconds, NetworkSide side)
		where T: Interaction
	{
		return interaction.Performer.Cooldown.TryStart(interactable, seconds, side);
	}

	/// <summary>
	/// Same as TryStart with NetworkSide.Client
	/// </summary>
	public static bool TryStartClient<T>(T interaction, IInteractable<T> interactable, float seconds)
		where T: Interaction
	{
		return TryStart(interaction, interactable, seconds, NetworkSide.Client);
	}

	/// <summary>
	/// Same as TryStart with NetworkSide.Server
	/// </summary>
	public static bool TryStartServer<T>(T interaction, IInteractable<T> interactable, float seconds)
		where T: Interaction
	{
		return TryStart(interaction, interactable, seconds, NetworkSide.Server);
	}

	/// <summary>
	/// Checks if the indicated cooldown is on (currently counting down) for the player
	/// </summary>
	/// <param name="cooldownId"></param>
	/// <returns></returns>
	public static bool IsOn(Mind player, CooldownID cooldownId)
	{
		return player.Cooldown.IsOn(cooldownId);
	}

	/// <summary>
	/// Checks if the indicated cooldown is on (currently counting down) for the performer of the interaction.
	/// </summary>
	/// <param name="cooldownId"></param>
	/// <returns></returns>
	public static bool IsOn(Interaction interaction, CooldownID cooldownId)
	{
		return IsOn(interaction.Performer, cooldownId);
	}

	/// <summary>
	/// Same as IsOn for for indicated clientside Cooldown
	/// </summary>
	public static bool IsOnClient(Mind player, ICooldown cooldown)
	{
		return IsOn(player, CooldownID.Asset(cooldown, NetworkSide.Client));
	}

	/// <summary>
	/// Same as IsOn for for indicated clientside Cooldown
	/// </summary>
	public static bool IsOnClient(Interaction interaction, ICooldown cooldown)
	{
		return IsOnClient(interaction.Performer, cooldown);
	}

	/// <summary>
	/// Same as IsOn for for indicated serverside Cooldown
	/// </summary>
	public static bool IsOnServer(Mind player, ICooldown cooldown)
	{
		return IsOn(player, CooldownID.Asset(cooldown, NetworkSide.Server));
	}

	/// <summary>
	/// Same as IsOn for for indicated serverside Cooldown
	/// </summary>
	public static bool IsOnServer(Interaction interaction, ICooldown cooldown)
	{
		return IsOnServer(interaction.Performer, cooldown);
	}
}
