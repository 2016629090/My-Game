using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace YY.RPGgame
{
    [CreateAssetMenu(fileName = "newSkill", menuName = "RPG/Skill")]
    public class SkillData : ScriptableObject
    {
        [Header("基础信息")]
        public string skillName;
        public string description;
        public Sprite icon;

        [Header("施法设置")]
        public float castTime = 0.5f;
        public bool requireTarget = false;
        public float range = 5f;

        [Header("冷却和消耗")]
        public float cooldown;
        public float manaCost = 10f;
        public float stimanaCost = 0f;

        [Header("动画配置")]
        public string animationTrigger = "Attack";
        public bool lockMovement = true;
        public float animationDuration = 1.0f;

        [Header("技能预制体")]
        public GameObject prefab;
        public Vector3 Pos;
        public Quaternion rotation;

        [Header("技能效果")]
        public List<SkillEffect> effects = new List<SkillEffect>();

        
    }
    [System.Serializable]
    public class SkillEffect
    {
        public enum EffectType
        {
            Damage,
            heal,
            buff
        }
        public EffectType type;
        public float value;
        public float duration;
        public GameObject prefab;
        public AudioClip audioEffect;
    }
}
