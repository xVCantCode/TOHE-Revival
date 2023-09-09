#nullable enable
using System;
using System.Linq;

using System.Threading;
using System.Threading.Tasks;

using InnerNet;
using UnityEngine;

using HarmonyLib;

using DiscordRPC;
using DiscordRPC.Logging;
using DiscordRPC.Message;
using Newtonsoft;
using static TOHE.Translator;

// Whole file gets placed into ToHE
namespace TOHE;
/// <summary>
/// A working DiscordRPC implementation for this mod
/// </summary>
[HarmonyPatch]
public static class DiscordRP
{
	public const string loggerTag = "Custom DiscordManager";
	private static DiscordRpcClient? client;
	
	public static void Update(bool immediately = false)
		=> new Task(() => UpdateReal(immediately)).Start();
	
	private static void UpdateReal(bool immediately)
	{
		Logger.Fatal("Presence attempt #" + UnityEngine.Random.RandomRangeInt(1, ushort.MaxValue), loggerTag);
		try
		{
			Logger.Fatal("Checking whether DiscordRpcClient exists...", loggerTag);

			if (client == null || client.IsDisposed)
				// If there is no client, give up
				return;

			Logger.Fatal("DiscordRpcClient exists.", loggerTag);

			Logger.Fatal("Checking whether AmongUsClient exists...", loggerTag);

			if (AmongUsClient.Instance == null)
			{
				Logger.Fatal("Missing AmongUsClient.", loggerTag);
				return;
			}

			Logger.Fatal("AmongUsClient exists.", loggerTag);

			// Make sure the thread is ran in parallel with the game, so it doesn't freeze anything
			Thread.CurrentThread.IsBackground = true;

			// If this is not an immediate request, wait 1 second before updating anything
			Logger.Fatal("Putting the thread to sleep.", loggerTag);
			if (!immediately)
				Thread.Sleep(1000);
			Logger.Fatal("Thread revived.", loggerTag);

			#region NULL
			// Convert the game code to a string
			string? gameCodeStr = null;
			if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.NotJoined && AmongUsClient.Instance.GameState != InnerNetClient.GameStates.Ended && AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
				// Set the game code string to the current game code if we are in an online lobby
				gameCodeStr = GameCode.IntToGameName(AmongUsClient.Instance.GameId);
			#endregion NULL
			Logger.Fatal($"Converted integer game ID '{AmongUsClient.Instance?.GameId ?? -1}' to a string game ID -{gameCodeStr}-", loggerTag);

			// Get images and text to use
			string LargeImage = GetString("LargeImageKey"); Logger.Fatal($"Defined string -LargeImage- as -{LargeImage}-", loggerTag);
			string LargeIText = GetString("LargeImageText"); Logger.Fatal($"Defined string -LargeIText- as -{LargeIText}-", loggerTag);

			string SmallImage = GetString("SmallImageKey"); Logger.Fatal($"Defined string -SmallImage- as -{SmallImage}-", loggerTag);
			string SmallIText = GetString("SmallImageText"); Logger.Fatal($"Defined string -SmallIText- as -{SmallIText}-", loggerTag);

			// Initialize a presence
			Logger.Fatal("<RichPresence>.ctor()", loggerTag);
			RichPresence presence = new();
			Party party = new()
			{
				Size = 0,
				Max = 0
			};
			Secrets secrets = new();
			presence.Assets = new DiscordRPC.Assets()
			{
				LargeImageKey = LargeImage,
				LargeImageText = LargeIText,
				SmallImageKey = SmallImage,
				SmallImageText = SmallIText
			};
			Logger.Fatal("Assets were set.", loggerTag);

			/* Presence format:
			 *	Application name
			 *	Details
			 *	State (<Party.CurrentSize> of <Party.MaxSize>)
			 *	<time> elapsed
			 */

			// -TODO- Add strings set in status as translations

			// Find what to do according to Innersloth's GameState
			Logger.Fatal("-switch- statement reached", loggerTag);
#pragma warning disable CS8602 // Visual Studio is dumb, the function is returned if AmongUsClient doesn't exist.
			switch (AmongUsClient.Instance.GameState)
#pragma warning restore CS8602
			{
				case InnerNetClient.GameStates.NotJoined:
					Logger.Fatal("-switch- statement fell into NotJoined gamestate", loggerTag);
					// We are in a menu
					presence.Details = "Creating a Game";
					presence.State = "In Menus";
					presence.Buttons = new Button[]
					{
					new Button() { Label = "Button A", Url = "https://example.com/1" },
					new Button() { Label = "Button B", Url = "https://example.com/2" }
					};
					break;

				case InnerNetClient.GameStates.Joined:
				case InnerNetClient.GameStates.Started:
					Logger.Fatal("-switch- statement fell into Joined/Started gamestate", loggerTag);
					// We are in a lobby or a game - Do things according to ToHE's state
					if (GameStates.IsInTask)
						// In game, not in any meetings
						presence.State = "Playing a game";

					if (GameStates.IsCountDown)
						// Counting down to the game start
						presence.State = "Starting in " + Mathf.CeilToInt(GameStartManager.Instance.countDownTimer) + " seconds";

					if (string.IsNullOrWhiteSpace(presence.Details))
						// Set the details to a default one depending on the network mode
						switch (AmongUsClient.Instance.NetworkMode)
						{
							case NetworkModes.LocalGame:
								Logger.Fatal("-switch- second statement fell ino LocalGame", loggerTag);
								presence.Details = "In a local game";
								break;

							case NetworkModes.OnlineGame:
								Logger.Fatal("-switch- second statement fell ino OnlineGame", loggerTag);
								// This is set somewhere else
								break;

							case NetworkModes.FreePlay:
								Logger.Fatal("-switch- second statement fell ino FreePlay", loggerTag);
								presence.Details = "Being a Noob";
								break;

							default:
								Logger.Fatal("-switch- second statement fell through all the way down to default", loggerTag);
								presence.Details = "Looking at a new";
								presence.State = "Among Us update";
								break;
						}
					break;

				case InnerNetClient.GameStates.Ended:
					Logger.Fatal("-switch- statement fell into Ended gamestate", loggerTag);
					// We're in a lobby, but the game has ended
					presence.Details = "Waiting for Host";
					presence.State = "Game Ended";
					break;

				default:
					Logger.Fatal("-switch- statement fell into default", loggerTag);
					presence.Details = "Looking at a new";
					presence.State = "Among Us update";
					break;
			}
			Logger.Fatal("-switch- statement finished", loggerTag);
			// Last things to do...
			if (AmongUsClient.Instance.GameState != InnerNetClient.GameStates.NotJoined)
			{
				Logger.Fatal("GameState is not NotJoined", loggerTag);
				// Not in the main menu

				if (GameStates.IsLobby)
				{
					presence.State = "In Lobby";
					client.UpdateButtons(null);
				}

				if (GameStates.IsMeeting)
				{
					Logger.Fatal("We are in a meeting", loggerTag);
					if (party.Max == 0 && party.Size == 0)
					{
						Logger.Fatal("The party size was not set - Setting to \"alive players/game setting max players\"", loggerTag);
						party.Size = Utils.AllAlivePlayersCount;
						party.Max = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers;
					}
					presence.State = "In a meeting";
				}

				if (party.Max == 0 && party.Size == 0)
				{
					Logger.Fatal("The party size was not set - Setting to \"all players/game setting max players\"", loggerTag);
					party.Size = GameStates.IsLobby ? GameStartManager.Instance.LastPlayerCount : Utils.AllPlayersCount;
					party.Max = GameOptionsManager.Instance.CurrentGameOptions.MaxPlayers; 
				}

				// Validate the payload
				if (party.Max is not 0 and <= 0)
				{
					Logger.Fatal($"Invalid max size, setting to 1.\t\t[{party.Max} -> 1]", loggerTag);
					party.Max = 1;
				}
				if (party.Size is not 0 and <= 0)
				{
					Logger.Fatal($"Invalid current size, setting to 1.\t[{party.Size} -> 1]", loggerTag);
					party.Size = 1;
				}

				if (AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame)
				{
					Logger.Fatal("We are in an online game, setting details, secrets and party fields to that...", loggerTag);
					presence.Details = $"Code is: {gameCodeStr ?? "XXYYZZ"}";
					//TODO:: IF join doesnt work..
					// *TODO* Find a way to create a join button (AmongUsHelper?), otherwise create a button with Label="Lobby code above"
					Logger.Fatal("Details setter pased!", loggerTag);
					secrets.JoinSecret = $"join{gameCodeStr ?? "XXYYZZ"}";
					Logger.Fatal("JoinSecret setter passed!", loggerTag);
#pragma warning disable CS0618 // This property might be obsolete, but Among Us uses it, and GameSDK is also obsolete.
					secrets.MatchSecret = $"match{gameCodeStr ?? "XXYYZZ"}";
#pragma warning restore CS0618
					Logger.Fatal("MatchSecret setter passed!", loggerTag);

					party.ID = gameCodeStr ?? "XXYYZZ";
					Logger.Fatal("Party ID setter passed!", loggerTag);
					party.Privacy = Party.PrivacySetting.Public;
					Logger.Fatal("Party Privacy setter passed!", loggerTag);
				}

				Logger.Fatal("Constructing a button...", loggerTag);
				if ((AmongUsClient.Instance.NetworkMode == NetworkModes.OnlineGame) && AmongUsClient.Instance.IsGameStarted)
				{
					presence.Buttons = new Button[]
					{
						new Button() { Label = "Lobby Info", Url = "https://discord.com/channels/1094344790910455908/1109931299562586173/1113512646247972946" }
					};
				}
			}

			Logger.Fatal("Sending presence to Discord...", loggerTag);
			// Finally, fix party and send an update to the client
			presence.Party = party;
			presence.Secrets = secrets;
			presence.Buttons = null;
			client.SetPresence(presence);
		}
		catch (Exception ex)
		{
			Logger.Fatal($"Error @ TOHE.DiscordRP$$Update({immediately})", loggerTag);
			Logger.Fatal("Stack trace:", loggerTag);
			string trace = ex.ToString();
			if (trace == null || string.IsNullOrWhiteSpace(trace))
				trace = "\tCouldn't get stack trace for an unknown reason";

			foreach (string line in trace.Split("\n"))
				Logger.Fatal(line, loggerTag);
		}
	}

	[HarmonyPatch(typeof(UnityEngine.SceneManagement.SceneManager), nameof(UnityEngine.SceneManagement.SceneManager.LoadScene), new Type[] { typeof(string) })]
	[HarmonyPostfix]
	private static void OnSceneChange([HarmonyArgument(0)] ref string newScene)
	{
		if (newScene == "MainMenu" || newScene == "MatchMaking" || newScene == "MMOnline" || newScene == "FindAGame")
			Update(true);
	}


	//[HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.FixedUpdate))]
	//[HarmonyPrefix]
	//private static bool FixedUpdate()
	//{
	//	client.Invoke();
	//	return false;
	//}

	[HarmonyPatch(typeof(DiscordManager), nameof(DiscordManager.Start))]
	[HarmonyPrefix]
	private static bool DiscordManager_Start(DiscordManager __instance)
	{
		// Disable the vanilla rich presence, if it exists
		if (__instance != null)
			__instance.presence = null;

		return false;
	}

	internal static void Initialize()
	{
		Logger.Info("Initializing the Discord client...", loggerTag);

		// Create a client
		client = new DiscordRpcClient("1111023738197119020"/*"477175586805252107"*/, -1, autoEvents: true);


		client.Logger = new ConsoleLogger(LogLevel.Trace, true);
		client.RegisterUriScheme("945360", "Among Us.exe");

		// [Like and] Subscribe to events

		// On ready, update (not working?!)
		client.OnReady += (object sender, ReadyMessage args) =>
		{
			Logger.Info("Discord is ready to be used", loggerTag);
			Update();
		};
		// On connection fail, log the error and give up
		client.OnConnectionFailed += (object sender, ConnectionFailedMessage msg) =>
		{
			Logger.Error("Connection to Discord failed! Is Discord running?", loggerTag);
			client.Dispose();
		};
		// On errror, log it
		client.OnError += (object sender, ErrorMessage err)
			=> Logger.Error($"Error received from Discord: '{err.Message}'", loggerTag);
		// On a join request, respond with 'Yes'
		client.OnJoinRequested += (object sender, JoinRequestMessage msg)
			=> client.Respond(msg, true);
		// On invite accept, join the lobby
		client.OnJoin += (object sender, JoinMessage msg) =>
		{
			if (!AmongUsClient.Instance)
			{
				Logger.Error("Missing Among Us client...", loggerTag);
				return;
			}

			string joinSecret = msg.Secret;

			if (!joinSecret.StartsWith("join"))
			{
				Logger.Warn($"Received an invalid join secret: '{joinSecret}' - Could be a newer version of Among Us?", loggerTag);
				return;
			}

			string gameCodeStr = joinSecret.Substring(4);
			if (gameCodeStr.Where(c => !char.IsLetter(c) || !char.IsUpper(c)).Any() || (gameCodeStr.Length != 4 && gameCodeStr.Length != 6))
			{
				Logger.Warn($"Received an invalid join secret: '{joinSecret}' - Could be a newer version of Among Us?", loggerTag);
				return;
			}

			int gameCodeInt = GameCode.GameNameToInt(gameCodeStr);
			if (AmongUsClient.Instance.GameId == gameCodeInt)
			{
				// Since the Among Us anticheat became more agressive, we must ignore this request if it is for the same lobby
				Logger.Warn("Received an invite to a lobby, but we already are in the lobby?", loggerTag);
				return;
			}

			// A game code managed to pass all of the checks, join it via a coroutine
			AmongUsClient.Instance.StartCoroutine(AmongUsClient.Instance.CoJoinOnlineGameFromCode(gameCodeInt));
		};

		// Finally, attempt to establish a connection
		client.Initialize();
	}
}
