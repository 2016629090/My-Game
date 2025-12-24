using System.Collections.Generic;
using UnityEngine;

namespace YY.RPGgame
{
    public class SkillManager : MonoBehaviour
    {
        [Header("技能配置")]
        [SerializeField] private List<SkillData> availableSkills = new List<SkillData>();// 玩家已学会的所有技能列表
        [SerializeField] private SkillSlot[] skillSlots;

        [Header("快捷栏设置")]
        public KeyCode[] quickCastKeys =
        {
            KeyCode.Alpha1,
            KeyCode.Alpha2,
            KeyCode.Alpha3,
            KeyCode.Alpha4
        };

        private Dictionary<string, SkillData> skillDictionary = new Dictionary<string, SkillData>();

        void Start()
        {
            InitializeSkills();// 初始化技能字典
            SetupDefaultSlots();// 设置默认技能到槽位
        }
        /// <summary>
        /// availableSkills 列表中的技能添加到字典，方便通过技能名快速查找。
        /// </summary>
        void InitializeSkills()
        {
            foreach (var skill in availableSkills)
            {
                if (!skillDictionary.ContainsKey(skill.skillName))
                {
                    skillDictionary.Add(skill.skillName, skill);
                }
            }
        }

        void SetupDefaultSlots()
        {
            for (int i = 0; i < Mathf.Min(availableSkills.Count, skillSlots.Length); i++)
            {
                skillSlots[i].AssignSkill(availableSkills[i]);
            }
        }

        public bool LearnSkill(SkillData skill)
        {
            if (!availableSkills.Contains(skill))
            {
                availableSkills.Add(skill);
                skillDictionary[skill.skillName] = skill;
                return true;
            }
            return false;
        }

        public bool AssignSkillToSlot(int slotIndex, SkillData skill)
        {
            if (slotIndex >= 0 && slotIndex < skillSlots.Length)
            {
                skillSlots[slotIndex].AssignSkill(skill);
                return true;
            }
            return false;
        }

        public bool UseSkill(int slotIndex, GameObject target = null)
        {
            if (slotIndex >= 0 && slotIndex < skillSlots.Length)
            {
                return skillSlots[slotIndex].TryUseSkill(gameObject, target);
            }
            return false;
        }

        void Update()
        {
            HandleQuickCast();
        }

        void HandleQuickCast()
        {
            for (int i = 0; i < quickCastKeys.Length; i++)
            {
                if (Input.GetKeyDown(quickCastKeys[i]))
                {
                    UseSkill(i);
                }
            }
        }

        public SkillSlot[] GetSkillSlots()
        {
            return skillSlots;
        }

        //通过技能名获取技能
        public SkillData GetSkillByName(string skillName)
        {
            if (skillDictionary.TryGetValue(skillName, out SkillData skill))
            {
                return skill;
            }
            return null;
        }

        // 从BaseEntity获取目标
        //private GameObject FindNearestTarget(float range)
        //{
        //    Collider[] colliders = Physics.OverlapSphere(transform.position, range);
        //    foreach (var collider in colliders)
        //    {
        //        var entity = collider.GetComponent<BaseEntity>();
        //        if (entity != null && entity.gameObject != gameObject)
        //        {
        //            return entity.gameObject;
        //        }
        //    }
        //    return null;
        //}
    }
}