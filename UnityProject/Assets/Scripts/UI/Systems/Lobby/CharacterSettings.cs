using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UI.CharacterCreator;

/// <summary>
/// Class containing all character preferences for a player
/// Includes appearance, job preferences etc...
/// </summary>
public class CharacterSettings
{
	// TODO: all of the in-game appearance variables should probably be refactored into a separate class which can
	// then be used in PlayerScript since job preferences are only needed at round start in ConnectedPlayer

	// IMPORTANT: these fields use primitive types (int, string... etc) so they can be sent  over the network with
	// RPCs and Commands without needing to serialise them to JSON!
	public const int MAX_NAME_LENGTH = 26; // Arbitrary limit, but 26 is the max the current UI can fit
	public string Username;
	public string Name = "Cuban Pete";
	public string AiName = "R.O.B.O.T.";
	public BodyType BodyType = BodyType.Male;
	public ClothingStyle ClothingStyle = ClothingStyle.JumpSuit;
	public BagStyle BagStyle = BagStyle.Backpack;
	public PlayerPronoun PlayerPronoun = PlayerPronoun.He_him;
	public int Age = 22;
	public Speech Speech = Speech.None;
	public string SkinTone = "#ffe0d1";
	public List<CustomisationStorage> SerialisedBodyPartCustom;
	public List<ExternalCustomisation> SerialisedExternalCustom;

	public string Species = "Human";
	public JobPrefsDict JobPreferences = new JobPrefsDict();
	public AntagPrefsDict AntagPreferences = new AntagPrefsDict();

	[Serializable]
	public class CustomisationClass
	{
		public string SelectedName = "None";
		public string Colour = "#ffffff";
	}

	public override string ToString()
	{
		var sb = new StringBuilder($"{Username}'s character settings:\n", 300);
		sb.AppendLine($"Name: {Name}");
		sb.AppendLine($"AiName: {AiName}");
		sb.AppendLine($"ClothingStyle: {ClothingStyle}");
		sb.AppendLine($"BagStyle: {BagStyle}");
		sb.AppendLine($"Pronouns: {PlayerPronoun}");
		sb.AppendLine($"Age: {Age}");
		sb.AppendLine($"Speech: {Speech}");
		sb.AppendLine($"SkinTone: {SkinTone}");
		sb.AppendLine($"JobPreferences: \n\t{string.Join("\n\t", JobPreferences)}");
		sb.AppendLine($"AntagPreferences: \n\t{string.Join("\n\t", AntagPreferences)}");
		return sb.ToString();
	}

	/// <summary>
	/// Does nothing if all the character's properties are valid
	/// <exception cref="InvalidOperationException">If the character settings are not valid</exception>
	/// </summary>
	public void ValidateSettings()
	{
		ValidateName();
		ValidateAiName();
		ValidateJobPreferences();
	}

	/// <summary>
	/// Checks if the character name follows all rules
	/// </summary>
	/// <exception cref="InvalidOperationException">If the name not valid</exception>
	private void ValidateName()
	{
		if (String.IsNullOrWhiteSpace(Name))
		{
			throw new InvalidOperationException("Name cannot be blank");
		}

		if (Name.Length > MAX_NAME_LENGTH)
		{
			throw new InvalidOperationException("Name cannot exceed " + MAX_NAME_LENGTH + " characters");
		}
	}

	/// <summary>
	/// Checks if the character Ai name follows all rules
	/// </summary>
	/// <exception cref="InvalidOperationException">If the name not valid</exception>
	private void ValidateAiName()
	{
		if (String.IsNullOrWhiteSpace(AiName))
		{
			AiName = "R.O.B.O.T.";
		}

		if (AiName.Length > MAX_NAME_LENGTH)
		{
			throw new InvalidOperationException("Name cannot exceed " + MAX_NAME_LENGTH + " characters");
		}
	}

	/// <summary>
	/// Checks if the job preferences have more than one high priority set
	/// </summary>
	/// <exception cref="InvalidOperationException">If the job preferences are not valid</exception>
	private void ValidateJobPreferences()
	{
		if (JobPreferences.Count(jobPref => jobPref.Value == Priority.High) > 1)
		{
			throw new InvalidOperationException("Cannot have more than one job set to high priority");
		}
	}


	public void SetPermanentName(string NewName)
	{
		Name = NewName;
	}

	private PlayerPronoun GetCorrectPlayerPronoun(bool IdentityVisible)
	{
		PlayerPronoun Pronoun = PlayerPronoun.They_them;
		if (IdentityVisible)
		{
			Pronoun = this.PlayerPronoun;
		}

		return Pronoun;
	}


	/// <summary>
	/// Returns a possessive string (i.e. "their", "his", "her") for the provided gender enum.
	/// </summary>
	public string TheirPronoun(bool IdentityVisible)
	{
		return TheirPronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}


	/// <summary>
	/// Returns a personal pronoun string (i.e. "he", "she", "they") for the provided gender enum.
	/// </summary>
	public string TheyPronoun(bool IdentityVisible)
	{
		return TheyPronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "him", "her", "them") for the provided gender enum.
	/// </summary>
	public string ThemPronoun(bool IdentityVisible)
	{
		return ThemPronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "he's", "she's", "they're") for the provided gender enum.
	/// </summary>
	public string TheyrePronoun(bool IdentityVisible)
	{
		return TheyrePronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "himself", "herself", "themself") for the provided gender enum.
	/// </summary>
	public string ThemselfPronoun(bool IdentityVisible)
	{
		return ThemselfPronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}

	public string IsPronoun(bool IdentityVisible)
	{
		return IsPronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}

	public string HasPronoun(bool IdentityVisible)
	{
		return HasPronoun(GetCorrectPlayerPronoun(IdentityVisible));
	}



	/// <summary>
	/// Returns a possessive string (i.e. "their", "his", "her") for the provided gender enum.
	/// </summary>
	public static string TheirPronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
				return "his";
			case PlayerPronoun.She_her:
				return "her";
			default:
				return "their";
		}
	}


	/// <summary>
	/// Returns a personal pronoun string (i.e. "he", "she", "they") for the provided gender enum.
	/// </summary>
	public static string TheyPronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
				return "he";
			case PlayerPronoun.She_her:
				return "she";
			default:
				return "they";
		}
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "him", "her", "them") for the provided gender enum.
	/// </summary>
	public static string ThemPronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
				return "him";
			case PlayerPronoun.She_her:
				return "her";
			default:
				return "them";
		}
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "he's", "she's", "they're") for the provided gender enum.
	/// </summary>
	public static string TheyrePronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
				return "he's";
			case PlayerPronoun.She_her:
				return "she's";
			default:
				return "they're";
		}
	}

	/// <summary>
	/// Returns an object pronoun string (i.e. "himself", "herself", "themself") for the provided gender enum.
	/// </summary>
	public static string ThemselfPronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
				return "himself";
			case PlayerPronoun.She_her:
				return "herself";
			default:
				return "themself";
		}
	}

	public static string IsPronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
			case PlayerPronoun.She_her:
				return "is";
			case PlayerPronoun.They_them:
			default:
				return "are";
		}
	}

	public static string HasPronoun(PlayerPronoun Pronoun)
	{
		switch (Pronoun)
		{
			case PlayerPronoun.He_him:
			case PlayerPronoun.She_her:
				return "has";
			case PlayerPronoun.They_them:
			default:
				return "have";
		}
	}
}
