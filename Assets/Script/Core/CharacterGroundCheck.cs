using UnityEngine;

/// <summary>
/// Kiểm tra player đứng trên mặt đất (CharacterController + raycast dự phòng).
/// </summary>
public static class CharacterGroundCheck
{
    public static bool IsGrounded(CharacterController controller, Transform body)
    {
        if (controller == null || body == null)
            return false;

        if (controller.isGrounded)
            return true;

        float rayLength = controller.height * 0.55f + 0.2f;
        Vector3 origin = body.position + Vector3.up * 0.1f;

        return Physics.Raycast(
            origin,
            Vector3.down,
            rayLength,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);
    }
}
