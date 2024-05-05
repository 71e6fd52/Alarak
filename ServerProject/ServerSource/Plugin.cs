using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Barotrauma;
using Barotrauma.Extensions;
using Microsoft.Xna.Framework;

namespace Alarak;

public partial class Plugin : IAssemblyPlugin
{
    private static readonly MethodBase rKill =
        LuaCsHook.ResolveMethod("Barotrauma.Character", "Kill", null);
    
    // private static readonly FieldInfo rCharacter = typeof(CharacterHealth).GetField("character", BindingFlags.Instance | BindingFlags.Public)!;

    private void InitServer()
    {
        GameMain.LuaCs.Hook.Patch(
            "Alarak_Kill",
            rKill,
            (instance, ptable) => {
                if (instance is not Character character) return null;
                if (!character.IsRemotePlayer) return null;
                if ((bool)ptable["isNetworkMessage"]) return null; // ignore if kill is not processed by the server
                if (character.IsDead || character.CharacterHealth.Unkillable || character.GodMode || character.Removed) { return null; }

                var crewManager = GameMain.GameSession.CrewManager;
                if (crewManager == null) return null;

                var bots = crewManager.GetCharacters().Where(c => !c.IsRemotePlayer && !c.isDead && !c.IsUnconscious).ToList();
                bots.Sort((a, b) => (a.WorldPosition - character.WorldPosition).LengthSquared().CompareTo((b.WorldPosition - character.WorldPosition).LengthSquared()));

                if (!bots.Any()) return null;
                ptable.PreventExecution = true;

                var bot = bots.First();
                
                ModUtils.Logging.PrintMessage(
                    $"[Alarak] {character.Name} cast Soul Absorption on {bot.Name}."
                );
                
                var botPos = new Vector2(bot.WorldPosition.X, bot.WorldPosition.Y);
                bot.TeleportTo(character.WorldPosition);
                character.TeleportTo(botPos);
                
                // (character.CharacterHealth, bot.CharacterHealth) = (bot.CharacterHealth, character.CharacterHealth);
                // rCharacter.SetValue(character.CharacterHealth, character);
                // rCharacter.SetValue(bot.CharacterHealth, bot);
                
                character.Revive(createNetworkEvent: true);
                bot.Kill(
                    (CauseOfDeathType)ptable["causeOfDeath"],
                    ptable["causeOfDeathAffliction"] as Affliction,
                    false, 
                    (bool)ptable["log"]
                );

                character.CharacterHealth.Unkillable = true; // prevent the character from being killed more than once in one tick
                
                GameMain.LuaCs.Timer.Wait(args => character.CharacterHealth.Unkillable = false, 1000);
                
                // Revive() and Kill() already send network events
                // GameMain.NetworkMember.CreateEntityEvent(character, new Character.CharacterStatusEventData());
                // GameMain.NetworkMember.CreateEntityEvent(bot, new Character.CharacterStatusEventData());

                return null;
            }
        );
    }

    private void DisposeServer()
    {
        GameMain.LuaCs.Hook.RemovePatch(
            "Alarak_Kill",
            rKill,
            LuaCsHook.HookMethodType.Before
        );
    }
}
