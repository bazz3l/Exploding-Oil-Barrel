using System.Collections.Generic; 
using System;
using Rust;
using UnityEngine;
using Oxide.Core.Plugins;
using Newtonsoft.Json.Linq;

namespace Oxide.Plugins
{
    [Info("Exploding Oil Barrel", "Bazz3l", "1.0.5")]
    [Description("Exploding oil barrels with explosion force, player damage and shake effect")]
    class ExplodingOilBarrels : RustPlugin
    {
        #region Fields
        private const string ExplosionEffect = "assets/bundled/prefabs/fx/explosions/explosion_03.prefab";
        private const string ShakeEffect     = "assets/bundled/prefabs/fx/takedamage_generic.prefab";
        private const string FireEffect      = "assets/bundled/prefabs/fx/gas_explosion_small.prefab";
        #endregion

        #region Config
        private PluginConfig config;

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        private PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                EnableShakeScreen      = true,
                EnableExplosionForce   = true,
                EnablePlayerDamage     = true,
                PlayerDamageDistance   = 2f,
                PlayerDamage           = 10f,
                ShakeDistance          = 20f,
                ExplosionForceDistance = 20f,
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
        void Init()
        {
            config = Config.ReadObject<PluginConfig>();
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null || entity.ShortPrefabName != "oil_barrel") return;

            string damageType = Enum.GetName(typeof(Rust.DamageType), info.damageTypes.GetMajorityDamageType());
            if (damageType != "Bullet") return;

            Effect.server.Run(ExplosionEffect, entity.transform.position, Vector3.zero, null, false);
            Effect.server.Run(FireEffect, entity.transform.position, Vector3.zero, null, false);

            ExplosionForce(entity);
            PlayerDamage(entity);
        }

        void ExplosionForce(BaseCombatEntity entity)
        {
            List<DroppedItem> Items = new List<DroppedItem>();

            Vis.Entities<DroppedItem>(entity.transform.position, config.ExplosionForceDistance, Items);

            Items.RemoveAll(item => item == null || item.IsDestroyed || !item.IsVisible(entity.transform.position));

            foreach(DroppedItem item in Items)
            {
                if (config.EnableExplosionForce)
                    item.GetComponent<Rigidbody>()?.AddExplosionForce(config.ExplosionForce, entity.transform.position, config.ExplosionForceDistance);
            }
        }

        void PlayerDamage(BaseCombatEntity entity)
        {
            List<BasePlayer> Players = new List<BasePlayer>();

            Vis.Entities<BasePlayer>(entity.transform.position, config.ShakeDistance, Players);

            foreach(BasePlayer player in Players)
            {
                if (player == null || !player.IsConnected) continue;

                Vector3 pos    = player.transform.position;
                float distance = Vector3.Distance(pos, entity.transform.position);

                if (config.EnablePlayerDamage && distance <= config.PlayerDamageDistance)
                    player.Hurt(config.PlayerDamage);

                if (!config.EnableShakeScreen) continue;

                for (int i = 0; i < 10; i++)
                    SendEffect(ShakeEffect, player);
            }
        }

        private void SendEffect(string name, BasePlayer player, Vector3 position = new Vector3(), Vector3 offset = new Vector3())
        {
            if (player == null || player.IsDead() || player.IsSleeping()) return;

            Effect.server.Run(name, player, 0, offset, position);
        }
        #endregion
    }
}
