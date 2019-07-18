using System;
using UnityEngine;
using Oxide.Core.Plugins;
using Newtonsoft.Json.Linq;
using System.Collections.Generic; 

namespace Oxide.Plugins
{
    [Info("Exploding Oil Barrel", "Bazz3l", "1.0.4")]
    [Description("Exploding oil barrels with explosion force, player damage and ground shake effect")]
    class ExplodingOilBarrels : RustPlugin
    {
        private const string ExplosionEffect = "assets/bundled/prefabs/fx/explosions/explosion_03.prefab";
        private const string FireEffect = "assets/bundled/prefabs/fx/gas_explosion_small.prefab"; 
        private const string ShakeEffect = "assets/prefabs/weapons/thompson/effects/attack_shake.prefab";

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
                EnableShakeScreen      = true,
                EnableExplosionForce   = true,
                EnablePlayerDamage     = true,
                PlayerDamageDistance   = 2f,
                PlayerDamage           = 10f,
                ShakeDistance          = 15f,
                ExplosionForceDistance = 15f,
                ExplosionForce         = 10f
            };
        }

        private class PluginConfig
        {
            public bool EnableShakeScreen;
            public bool EnableExplosionForce;
            public bool EnablePlayerDamage;
            public float PlayerDamageDistance;
            public float PlayerDamage;
            public float ShakeDistance;
            public float ExplosionForce;
            public float ExplosionForceDistance;
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

            Effect.server.Run(ExplosionEffect, entity.transform.position, Vector3.zero, null, false);
            Effect.server.Run(FireEffect, entity.transform.position, Vector3.zero, null, false);

            List<DroppedItem> ItemsDropped = new List<DroppedItem>();
            Vis.Entities<DroppedItem>(entity.transform.position, configData.ExplosionDistance, ItemsDropped);
            ItemsDropped.RemoveAll(item => item == null || item.IsDestroyed || !item.IsVisible(entity.transform.position));
            foreach(DroppedItem item in ItemsDropped)
            {
                if (configData.EnableExplosionForce)
                    item?.GetComponent<Rigidbody>()?.AddExplosionForce(configData.ExplosionForce, entity.transform.position, configData.ExplosionForceDistance);
            }

            List<BasePlayer> NearPlayers = new List<BasePlayer>();
            Vis.Entities<BasePlayer>(entity.transform.position, configData.ShakeDistance, NearPlayers);
            foreach(var player in NearPlayers)
            {
                if (player != null && player.IsConnected)
                {
                    if (configData.EnablePlayerDamage && Vector3.Distance(player.transform.position, entity.transform.position) <= configData.PlayerDamageDistance)
                        player.Hurt(configData.PlayerDamage);

                    if (configData.EnableShakeScreen && !player.IsDead())
                    {
                        for (int i = 0; i < 10; i++)
                           Effect.server.Run(ShakeEffect, player.transform.position);
                    }                  
                }
            }
        }
        #endregion
    }
}
