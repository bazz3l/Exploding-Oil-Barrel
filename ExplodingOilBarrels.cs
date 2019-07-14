using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Newtonsoft.Json.Linq;
using System.Collections.Generic; 

namespace Oxide.Plugins
{
    [Info("Exploding Oil Barrel", "Bazz3l", "1.0.2")]
    [Description("Exploding oil barrels with ground shake effect")]
    class ExplodingOilBarrels : RustPlugin
    {
        private const string BarrelEffect = "assets/bundled/prefabs/fx/explosions/explosion_03.prefab";
        private const string ShakeEffect  = "assets/prefabs/weapons/thompson/effects/attack_shake.prefab";

        #region Config
        private PluginConfig configData;

        protected override void LoadDefaultConfig()
        {
            Config.WriteObject(GetDefaultConfig(), true);
        }

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                ShakeScreen   = true,
                ShakeDistance = 15
            };
        }

        private class PluginConfig
        {
            public bool ShakeScreen;
            public int ShakeDistance;
        }
        #endregion 

        #region Oxide
        private void Init() => configData = Config.ReadObject<PluginConfig>();

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
            {
                return;
            }

            if (entity.ShortPrefabName != "oil_barrel")
            {
                return;
            }

            string damageType = Enum.GetName(typeof(Rust.DamageType), info.damageTypes.GetMajorityDamageType());
            if (damageType != "Bullet")
            {
                return;
            }

            Effect.server.Run(BarrelEffect, entity.transform.position, Vector3.zero, null, false);

            List<BasePlayer> NearPlayers = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(entity.transform.position, configData.ShakeDistance, NearPlayers);

            foreach(var player in NearPlayers)
            {
                if (player != null && player.IsConnected && configData.ShakeScreen)
                {
                    for (int i = 0; i < 10; i++)
                        Effect.server.Run(ShakeEffect, player.transform.position);
                }
            }
        }
        #endregion
    }
}