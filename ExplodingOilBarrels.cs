using System.Collections.Generic; 
using Rust;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Exploding Oil Barrel", "Bazz3l", "1.0.6")]
    [Description("Exploding oil barrels with explosion force, player damage and ground shake effect")]
    class ExplodingOilBarrels : RustPlugin
    {
        const string _explosionEffect = "assets/bundled/prefabs/fx/explosions/explosion_03.prefab";
        const string _fireEffect = "assets/bundled/prefabs/fx/gas_explosion_small.prefab"; 
        const string _shakeEffect = "assets/prefabs/weapons/thompson/effects/attack_shake.prefab";

        #region Config
        PluginConfig _config;

        protected override void LoadDefaultConfig() => Config.WriteObject(GetDefaultConfig(), true);

        PluginConfig GetDefaultConfig()
        {
            return new PluginConfig
            {
                EnableShakeScreen = true,
                EnableExplosionForce = true,
                EnablePlayerDamage = true,
                PlayerDamageDistance = 2f,
                PlayerDamage = 10f,
                ShakeDistance = 20f,
                ExplosionDistance = 20f,
                ExplosionForce = 50f
            };
        }

        class PluginConfig
        {
            public bool EnableShakeScreen;
            public bool EnableExplosionForce;
            public bool EnablePlayerDamage;
            public float PlayerDamageDistance;
            public float PlayerDamage;
            public float ShakeDistance;
            public float ExplosionForce;
            public float ExplosionDistance;
        }
        #endregion 

        #region Oxide
        void Init()
        {
            _config = Config.ReadObject<PluginConfig>();
        }

        void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null) return;
            if (entity.ShortPrefabName != "oil_barrel" || info.damageTypes.GetMajorityDamageType() != DamageType.Bullet) return;

            PlayExplosion(entity.transform.position);
            PlayerInRange(entity.transform.position);

            if (_config.EnableExplosionForce)
            {
                MoveItems(entity.transform.position);
            }
        }
        #endregion

        #region Core
        void PlayExplosion(Vector3 position)
        {
            Effect.server.Run(_explosionEffect, position, Vector3.zero, null, false);
            Effect.server.Run(_fireEffect, position, Vector3.zero, null, false);
        }

        void MoveItems(Vector3 position)
        {
            List<DroppedItem> droppedItems = new List<DroppedItem>();

            Vis.Entities<DroppedItem>(position, _config.ExplosionDistance, droppedItems);

            droppedItems.RemoveAll(item => item == null || item.IsDestroyed || !item.IsVisible(position));

            foreach(DroppedItem item in droppedItems)
            {
                item.GetComponent<Rigidbody>()?.AddExplosionForce(_config.ExplosionForce, position, _config.ExplosionDistance);
            }
        }

        void PlayerInRange(Vector3 position)
        {
            List<BasePlayer> players = new List<BasePlayer>();

            Vis.Entities<BasePlayer>(position, _config.ShakeDistance, players);

            foreach(BasePlayer player in players)
            {
                if (_config.EnablePlayerDamage && Vector3.Distance(player.transform.position, position) <= _config.PlayerDamageDistance)
                {
                    DamagePlayer(player, position);
                }

                if (_config.EnableShakeScreen)
                {
                    PlayerShake(player);
                }
            }
        }

        void DamagePlayer(BasePlayer player, Vector3 position) => player.Hurt(_config.PlayerDamage);

        void PlayerShake(BasePlayer player)
        {
            for (int i = 0; i < 5; i++)
            {
                Effect.server.Run(_shakeEffect, player.transform.position);
            }
        }
        #endregion
    }
}
