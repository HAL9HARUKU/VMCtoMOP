using System.Collections;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class MopSender : MonoBehaviour
{
    [SerializeField]
    private Animator animator;
    private Transform cachedTransform;
    [SerializeField]
    private OscClient oscClient;
    private uOSC.Bundle sendBundle = new uOSC.Bundle();

    private bool isRunning = true;
    
    private void Start()
    {
        this.cachedTransform = this.animator.transform;

        RunMotionCapture().Forget();
    }
    private void OnApplicationQuit()
    {
        End();
    }
    private void OnDestroy()
    {
        End();
    }
    private void End()
    {
        this.isRunning = false;
    }
    async private UniTaskVoid RunMotionCapture()
    {
        await UniTask.Yield(PlayerLoopTiming.Update);

        // T ポーズにして体格を計測する
        var controller = this.animator.runtimeAnimatorController;
        this.animator.runtimeAnimatorController = null;

        // 次のフレームまで待機
        await UniTask.Yield(PlayerLoopTiming.Update);

        MeasureBodySize();

        // アニメーションを元に戻す
        this.animator.runtimeAnimatorController = controller;

        // 次のフレームまで待機
        await UniTask.Yield(PlayerLoopTiming.Update);
        
        // 送信
        while (this.isRunning)
        {
            SendMotion();
            
            // 次のフレームまで待機
            await UniTask.Yield(PlayerLoopTiming.Update);
        }
    }

    private void SendMotion()
    {
        // バンドル作成
        this.sendBundle = new uOSC.Bundle();
        
        // Body
        PostBodySize();
        // Bones
        PostBones();
        
        // 送信
        this.oscClient.Send(this.sendBundle);
        this.sendBundle = null;
    }

    private const int BoneParameterNum = 15;
    private static readonly string[] BoneNameList = new string[BoneParameterNum]{"pelvis", "spine", "upperarm_l", "upperarm_r", "lowerarm_l", "lowerarm_r", "hand_l", "hand_r", "neck", "thigh_l", "thigh_r", "calf_l", "calf_r", "foot_l", "foot_r"};

    private Vector3[] skeletonParameterList = new Vector3 [BoneParameterNum];
    
    private void MeasureBodySize()
    {
        var parameterIndex = 0;
        var pelvis = this.animator.GetBoneTransform(HumanBodyBones.Hips);
        var spine = this.animator.GetBoneTransform(HumanBodyBones.UpperChest);
        if (spine == null){ spine = this.animator.GetBoneTransform(HumanBodyBones.Chest); }
        if (spine == null){ spine = this.animator.GetBoneTransform(HumanBodyBones.Spine); }
        var leftUpperArm = this.animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
        var rightUpperArm = this.animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
        var leftLowerArm = this.animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
        var rightLowerArm = this.animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
        var leftHand = this.animator.GetBoneTransform(HumanBodyBones.LeftHand);
        var rightHand = this.animator.GetBoneTransform(HumanBodyBones.RightHand);
        var neck = this.animator.GetBoneTransform(HumanBodyBones.Neck);
        var leftThigh = this.animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        var rightThigh = this.animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        var leftCalf = this.animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        var rightCalf = this.animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        var leftFoot = this.animator.GetBoneTransform(HumanBodyBones.LeftFoot);
        var rightFoot = this.animator.GetBoneTransform(HumanBodyBones.RightFoot);
        
        // pelvis
        this.skeletonParameterList[parameterIndex++] = pelvis.position;
        // spine
        this.skeletonParameterList[parameterIndex++] = spine.position;
        // upperarm_l
        this.skeletonParameterList[parameterIndex++] = leftUpperArm.position;
        // upperarm_r
        this.skeletonParameterList[parameterIndex++] = rightUpperArm.position;
        // lowerarm_l
        this.skeletonParameterList[parameterIndex++] = leftLowerArm.position;
        // lowerarm_r
        this.skeletonParameterList[parameterIndex++] = rightLowerArm.position;
        // hand_l
        this.skeletonParameterList[parameterIndex++] = leftHand.position;
        // hand_r
        this.skeletonParameterList[parameterIndex++] = rightHand.position;
        // neck
        this.skeletonParameterList[parameterIndex++] = neck.position;
        // calf_l
        this.skeletonParameterList[parameterIndex++] = leftCalf.position;
        // calf_r
        this.skeletonParameterList[parameterIndex++] = rightCalf.position;
        // foot_l
        this.skeletonParameterList[parameterIndex++] = leftFoot.position;
        // foot_r
        this.skeletonParameterList[parameterIndex++] = rightFoot.position;

        // // Debug
        // var sb = new System.Text.StringBuilder();
        // for (var index = 0; index < BoneParameterNum; ++index)
        // {
        //     sb.Append($"{BoneNameList[index]} = {this.skeletonParameterList[index]}").Append($"\n");
        // }
        // Debug.Log($"{sb}");
    }
    private void PostBodySize()
    {
        for (var parameterIndex = 0; parameterIndex < BoneParameterNum; ++parameterIndex)
        {
            var parameter = skeletonParameterList[parameterIndex];
            var message = new uOSC.Message($"/Mop/Skeleton/{BoneNameList[parameterIndex]}",
                new object[3]{-parameter.x * 100.0f, parameter.z * 100.0f, parameter.y * 100.0f});
            this.sendBundle.Add(message);
        }
    }
    private static readonly HumanBodyBones[] BoneList = new HumanBodyBones[BoneParameterNum]{HumanBodyBones.Hips, HumanBodyBones.UpperChest, HumanBodyBones.LeftUpperArm, HumanBodyBones.RightUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightLowerArm, HumanBodyBones.LeftHand, HumanBodyBones.RightHand, HumanBodyBones.Neck, HumanBodyBones.LeftUpperLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightLowerLeg, HumanBodyBones.LeftFoot, HumanBodyBones.RightFoot};

    private void PostBones()
    {
        // var sb = new System.Text.StringBuilder();

        for (var parameterIndex = 0; parameterIndex < BoneParameterNum; ++parameterIndex)
        {
            var location = Vector3.zero;
            var rotation = Quaternion.identity;
            var transform = this.animator.GetBoneTransform(BoneList[parameterIndex]);
            if (transform == null && parameterIndex == (int)HumanBodyBones.UpperChest)
            {
                transform = this.animator.GetBoneTransform(HumanBodyBones.Chest);
                if (transform == null){ transform = this.animator.GetBoneTransform(HumanBodyBones.Spine); }
            }
            
            if (transform != null)
            {
                location = transform.position;
                rotation = transform.rotation;
            }
            
            this.sendBundle.Add(new uOSC.Message($"/Mop/BoneControl/{BoneNameList[parameterIndex]}",
                -location.x * 100.0f, location.z * 100.0f, location.y * 100.0f,
                -rotation.x, rotation.z, rotation.y, rotation.w)
            );
            
            // sb.Append($"{BoneNameList[parameterIndex]} = {transform.position}").Append($"\n");
        }

        // // Debug
        // Debug.Log($"{sb}");
    }
}
