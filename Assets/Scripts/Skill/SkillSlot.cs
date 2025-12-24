using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace YY.RPGgame
{
    public class SkillSlot : MonoBehaviour
    {
        [SerializeField] private int slotIndex;
        [SerializeField] private SkillData skillData;

        private float currentCooldown = 0f;
        private bool isOnCooldown = false;

        [Header("事件")]
        public UnityEvent<SkillData> onSkillUsed;
        public UnityEvent<float> onCooldownChanged; // 参数: 剩余冷却时间百分比

        public SkillData SkillData => skillData;
        public float CooldownPercent => currentCooldown / skillData.cooldown;
        public bool IsReady => !isOnCooldown;

        public void AssignSkill(SkillData newSkill)
        {
            skillData = newSkill;

            if(newSkill.icon != null)
            {
                this.transform.GetChild(0).GetComponent<Image>().sprite = newSkill.icon;
            }
            currentCooldown = 0f;
            isOnCooldown = false;
        }

        public bool TryUseSkill(GameObject caster, GameObject target = null)
        {
            if (isOnCooldown || skillData == null) return false;

            // 检查消耗
            if (!CheckCost(caster)) return false;

            // 施放技能
            StartCoroutine(UseSkillCoroutine(caster, target));
            return true;
        }

        private bool CheckCost(GameObject caster)
        {
            // 这里可以检查MP、耐力等
            return true;
        }

        private System.Collections.IEnumerator UseSkillCoroutine(GameObject caster, GameObject target)
        {
            // 施法前摇
            if (skillData.castTime > 0)
            {
                yield return new WaitForSeconds(skillData.castTime);
            }

            // 应用所有效果
            foreach (var effect in skillData.effects)
            {
                //ApplyEffect(effect, caster, target);
            }

            // 触发事件
            onSkillUsed?.Invoke(skillData);
            // 开始冷却
            StartCooldown();
        }

        //private void ApplyEffect(SkillEffect effect, GameObject caster, GameObject target)
        //{
        //    switch (effect.type)
        //    {
        //        case SkillEffect.EffectType.Damage:
        //            if (target != null)
        //            {
        //                var health = target.GetComponent<BaseEntity>();
        //                if (health != null) health.TakeDamage(effect.value);
        //            }
        //            break;

        //        case SkillEffect.EffectType.Heal:
        //            var casterHealth = caster.GetComponent<BaseEntity>();
        //            if (casterHealth != null) casterHealth.Heal(effect.value);
        //            break;

        //        case SkillEffect.EffectType.Projectile:
        //            if (effect.prefab != null)
        //            {
        //                Vector3 spawnPos = caster.transform.position + caster.transform.forward;
        //                GameObject projectile = Instantiate(effect.prefab, spawnPos, caster.transform.rotation);

        //                var proj = projectile.GetComponent<Projectile>();
        //                if (proj != null && target != null)
        //                {
        //                    proj.SetTarget(target.transform);
        //                }
        //            }
        //            break;
        //    }

        //    // 播放音效
        //    if (effect.soundEffect != null)
        //    {
        //        AudioSource.PlayClipAtPoint(effect.soundEffect, caster.transform.position);
        //    }
        //}

        private void StartCooldown()
        {
            isOnCooldown = true;
            currentCooldown = skillData.cooldown;
            StartCoroutine(CooldownCoroutine());
        }

        private System.Collections.IEnumerator CooldownCoroutine()
        {
            while (currentCooldown > 0)
            {
                currentCooldown -= Time.deltaTime;
                transform.GetChild(1).GetComponent<Image>().fillAmount = currentCooldown / skillData.cooldown;
                onCooldownChanged?.Invoke(CooldownPercent);
                yield return null;
            }

            currentCooldown = 0f;
            isOnCooldown = false;
            onCooldownChanged?.Invoke(0f);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + slotIndex) && skillData != null)
            {
                TryUseSkill(gameObject);
            }
        }
    }
}