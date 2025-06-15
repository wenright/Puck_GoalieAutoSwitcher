using System;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace GoalieAutoSwitcher
{
  public class Class1 : IPuckMod
  {
    static readonly Harmony harmony = new Harmony("wenright.GoalieAutoSwitcher");

    [HarmonyPatch(typeof(Puck), "FixedUpdate")]
    public class CheckPuckPosition
    {
      [HarmonyPrefix]
      public static void Postfix(Puck __instance)
      {
        if (!NetworkManager.Singleton.IsServer) return;
        
        // We only want to enable switching if there are an odd number of players, and only 1 goalie
        List<Player> players = PlayerManager.Instance.GetPlayers().FindAll(player => player.Team.Value == PlayerTeam.Blue ||  player.Team.Value == PlayerTeam.Red);
        List<Player> goalies = players.FindAll(player => player.Role.Value == PlayerRole.Goalie);
        if (players.Count % 2 == 0 || goalies.Count != 1) return;
        if (PuckManager.Instance.GetPucks().Count != 1) return;
        
        Player goalie = goalies[0];
        PlayerTeam intendedTeam;
        if (__instance.transform.position.z > 2.0f)
        {
          intendedTeam = PlayerTeam.Blue;
        }
        else if(__instance.transform.position.z < -2.0f)
        {
          intendedTeam = PlayerTeam.Red;
        }
        else
        {
          return;
        }
        
        // No need to update if we're already on the right team
        if (goalie.Team.Value == intendedTeam) return;
        
        float originalMidMatchDelay =
          ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.joinMidMatchDelay;
        ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.joinMidMatchDelay = 0;
        
        goalie.Team.Value = intendedTeam;

        PlayerPosition intendedPosition = PlayerPositionManager.Instance.AllPositions.Find(position =>
          position.Role == PlayerRole.Goalie && position.Team == intendedTeam);
        intendedPosition.Server_Claim(goalie);

        ServerManager.Instance.ServerConfigurationManager.ServerConfiguration.joinMidMatchDelay = originalMidMatchDelay;
      }
    }
    
    public bool OnEnable()
    {
      try
      {
        // Patched all functions we have defined to be patched
        harmony.PatchAll();
      }
      catch (Exception e)
      {
        Debug.LogError($"Harmony patch failed: {e.Message}");

        return false;
      }

      return true;
    }

    public bool OnDisable()
    {
      try
      {
        // Reverts our patches, essentially returning the game to a vanilla state
        harmony.UnpatchSelf();
      }
      catch (Exception e)
      {
        Debug.LogError($"Harmony unpatch failed: {e.Message}");

        return false;
      }
      
      return true;
    }
  }
}