using System.Text.Json.Serialization;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using DamageManagement.API;
using Microsoft.Extensions.Logging;

namespace DamageManagement
{
    public class Config : BasePluginConfig
    {
        [JsonPropertyName("enable_inflictors")]
        public string[] listWeapons { get; set; } =
            [
                "inferno",
                "hegrenade_projectile",
                "flashbang_projectile",
                "smokegrenade_projectile",
                "decoy_projectile",
                "planted_c4",
            ];
    }

    public partial class DamageManagement : BasePlugin, IPluginConfig<Config>
    {
        public override string ModuleName => "CS2-DamageManagement";

        public override string ModuleVersion => "1.2";
        public override string ModuleAuthor => "HoanNK";
        public Config Config { get; set; } = new Config();
        static bool isSlayCommand { get; set; }

        public FakeConVar<bool> FaceitDmgEnabled = new(
            "faceit_dmg_enable",
            "Enable faceit alike team damage",
            true
        );
        public bool IsFaceitDmgEnabled = true;

        //This contains all designer name of the weapon class that allow to be taken damage to the teammates
        //Designer name can be found here https://cs2.poggu.me/dumped-data/entity-list
        string[] enableDmgInflictors = [];

        //Shared API
        public DamageManagementAPI managementAPI { get; set; } = new();
        private static PluginCapability<IDamageManagementAPI> DamageManagemenCapability { get; } =
            new("damagemanagement:api");

        public override void Load(bool hotReload)
        {
            managementAPI = new DamageManagementAPI();
            Capabilities.RegisterPluginCapability(DamageManagemenCapability, () => managementAPI);
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

            IsFaceitDmgEnabled = FaceitDmgEnabled.Value;
            FaceitDmgEnabled.ValueChanged += (sender, value) =>
            {
                IsFaceitDmgEnabled = value;
            };
        }

        public void OnConfigParsed(Config config)
        {
            enableDmgInflictors = config.listWeapons;
            if (enableDmgInflictors.Length == 0)
            {
                enableDmgInflictors =
                [
                    "inferno",
                    "hegrenade_projectile",
                    "flashbang_projectile",
                    "smokegrenade_projectile",
                    "decoy_projectile",
                    "planted_c4",
                ];
            }
        }

        public override void Unload(bool hotReload)
        {
            VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
        }

        private HookResult OnTakeDamage(DynamicHook hook)
        {
            var ExecuteOriginalMethod = managementAPI.IsNeedCallOriginalMethod();
            if (ExecuteOriginalMethod)
            {
                return HookResult.Continue;
            }
            if (IsFaceitDmgEnabled)
            {
                try
                {
                    var victim = hook.GetParam<CEntityInstance>(0);
                    var damageInfo = hook.GetParam<CTakeDamageInfo>(1);
                    if (damageInfo.Inflictor.Value == null || damageInfo.Attacker.Value == null)
                        return HookResult.Continue;

                    string inflictor = damageInfo.Inflictor.Value.DesignerName ?? "";
                    var attackPlayer = new CCSPlayerPawn(damageInfo.Attacker.Value.Handle);
                    var playerTakenDmg = new CCSPlayerController(victim.Handle);

                    if (
                        attackPlayer.TeamNum == playerTakenDmg.TeamNum
                        && "player".Equals(victim.DesignerName)
                    )
                    {
                        if (enableDmgInflictors.Contains(inflictor))
                        {
                            isSlayCommand = false;
                            return HookResult.Continue;
                        }
                        return HookResult.Handled;
                    }
                    return HookResult.Continue;
                }
                catch (Exception ex)
                {
                    Logger.LogError("{@error}", ex);
                }
                return HookResult.Continue;
            }
            return HookResult.Continue;
        }
    }
}
