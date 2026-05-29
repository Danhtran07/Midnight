using UnityEngine;

/// <summary>
/// Tìm xương humanoid / mixamo trên model player.
/// </summary>
public static class HumanoidBoneUtility
{
    public struct FootBones
    {
        public Transform LeftFoot;
        public Transform RightFoot;
        public Transform Hips;
    }

    public static FootBones Resolve(
        Animator animator,
        Transform searchRoot,
        Transform assignedLeft,
        Transform assignedRight,
        Transform assignedHips)
    {
        var result = new FootBones
        {
            LeftFoot = assignedLeft,
            RightFoot = assignedRight,
            Hips = assignedHips
        };

        if (result.Hips == null)
            result.Hips = TransformHierarchyUtility.FindChildByName(searchRoot, "mixamorig:Hips")
                ?? TransformHierarchyUtility.FindChildByName(searchRoot, "Hips");

        if (animator != null && animator.isHuman)
        {
            if (result.LeftFoot == null)
                result.LeftFoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            if (result.RightFoot == null)
                result.RightFoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            if (result.Hips == null)
                result.Hips = animator.GetBoneTransform(HumanBodyBones.Hips);
        }

        if (result.LeftFoot == null)
            result.LeftFoot = TransformHierarchyUtility.FindChildByName(searchRoot, "mixamorig:LeftFoot")
                ?? TransformHierarchyUtility.FindChildByName(searchRoot, "LeftFoot");

        if (result.RightFoot == null)
            result.RightFoot = TransformHierarchyUtility.FindChildByName(searchRoot, "mixamorig:RightFoot")
                ?? TransformHierarchyUtility.FindChildByName(searchRoot, "RightFoot");

        return result;
    }
}
