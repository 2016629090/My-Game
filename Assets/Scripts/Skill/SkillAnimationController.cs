using UnityEngine;
using UnityEngine.Events;

namespace YY.RPGgame
{
    public class SkillAnimationController : MonoBehaviour
    {
        [Header("组件引用")]
        [SerializeField] private Animator animator;
        [SerializeField] private SkillManager skillManager;
        [SerializeField] private PlayerMove playerMove; // 用于锁定移动

        [Header("事件")]
        public UnityEvent<string> onAnimationStart; // 参数: 技能名
        public UnityEvent<string> onAnimationEnd;   // 参数: 技能名

        private Coroutine currentAnimationCoroutine;

        private SkillData skillData;

        private void Start()
        {
            // 自动获取组件
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (skillManager == null) skillManager = GetComponent<SkillManager>();
            if (playerMove == null) playerMove = GetComponent<PlayerMove>();

            // 监听技能使用事件
            foreach (var slot in skillManager.GetSkillSlots())
            {
                if (slot != null)
                {
                    slot.onSkillUsed.AddListener(PlaySkillAnimation);
                }
            }
        }

        // 核心方法：播放技能动画
        public void PlaySkillAnimation(SkillData skill)
        {
            if (skill == null || animator == null) return;

            skillData = skill;

            // 1. 触发事件
            onAnimationStart?.Invoke(skill.skillName);

            // 2. 锁定移动
            if (skill.lockMovement && playerMove != null)
            {
                //playerMove.SetMovementLock(true);
            }

            // 3. 播放动画
            if (!string.IsNullOrEmpty(skill.animationTrigger))
            {
                animator.SetTrigger(skill.animationTrigger);
            }

            // 4. 开始恢复协程
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
            }
            currentAnimationCoroutine = StartCoroutine(AnimationRecovery(skill));
        }

        // 动画恢复协程
        private System.Collections.IEnumerator AnimationRecovery(SkillData skill)
        {
            // 等待动画完成
            yield return new WaitForSeconds(skill.animationDuration);

            // 恢复移动
            if (skill.lockMovement && playerMove != null)
            {
                //playerMove.SetMovementLock(false);
            }

            // 触发结束事件
            onAnimationEnd?.Invoke(skill.skillName);

            currentAnimationCoroutine = null;
        }

        // 手动停止当前动画
        public void StopCurrentAnimation()
        {
            if (currentAnimationCoroutine != null)
            {
                StopCoroutine(currentAnimationCoroutine);
                currentAnimationCoroutine = null;
            }

            if (playerMove != null)
            {
                //playerMove.SetMovementLock(false);
            }
        }
        public void OnAnimationEvent(int eventId)
        {
            if(skillData != null)
            {
                SkillEffect effects = skillData.GetEffectById(eventId);
                Debug.Log(effects.Pos);

                Vector3 worldPosition = transform.position + effects.Pos;
                GameObject effectObj = Instantiate(effects.prefab, worldPosition, Quaternion.identity);

                Quaternion characterYRotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
                Quaternion skillXRotation = Quaternion.Euler(transform.eulerAngles.x, 0, 0);
                Quaternion skillZRotation = Quaternion.Euler(0, 0, effects.rotation.eulerAngles.z);

                //effect.transform.rotation = skillData.rotation * transform.rotation;
                effectObj.transform.rotation = characterYRotation * skillXRotation * skillZRotation;
            }
        }
    }
}