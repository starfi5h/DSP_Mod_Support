using UnityEngine;

namespace CameraTools
{
	public class FreePointPoser
	{
		readonly float rotateSens = 1.0f;
		readonly float moveSens = 1.0f;
		readonly float damp = 0.20f;

		float pitchWanted;
		float yawWanted;		
		float rollWanted;
		float xWanted;
		float yWanted;
		float zWanted;

		bool enabled;

		public bool Enabled
        {
			get => enabled;

			set
            {
				enabled = value;
				yawWanted = pitchWanted = rollWanted = 0;
				xWanted = yWanted = zWanted = 0;
			}
		}

		public void Calculate(ref CameraPose cameraPose)
		{
			if (VFInput.inFullscreenGUI) return;

			// ThirdPersonPoser, PlanetPoser
			if (VFInput._cameraRTSRotateButton.pressing)
			{
				yawWanted += VFInput.mouseMoveAxis.x * 5f * rotateSens * GameCamera.camRotSensX;
				pitchWanted += VFInput.mouseMoveAxis.y * 5f * rotateSens * GameCamera.camRotSensY;
			}
			if (VFInput._cameraRTSRollButton.pressing)
			{
				rollWanted += VFInput.mouseMoveAxis.x * 5f * rotateSens * GameCamera.camRotSensX;
				rollWanted -= VFInput.mouseMoveAxis.y * 5f * rotateSens * GameCamera.camRotSensY;
			}
			yawWanted += VFInput.camJoystickAxis.x * 2f * rotateSens;
			pitchWanted += VFInput.camJoystickAxis.y * 0.5f * rotateSens;

			yawWanted = Mathf.Clamp(yawWanted, -89.9f, 89.9f);
			pitchWanted = Mathf.Clamp(pitchWanted, -89.9f, 89.9f);
			float yaw = Mathf.LerpAngle(0f, yawWanted, damp);
			float pitch = Mathf.LerpAngle(0f, pitchWanted, damp);
			float roll = Mathf.LerpAngle(0f, rollWanted, damp);
			yawWanted -= yaw;
			pitchWanted -= pitch;
			rollWanted -= roll;

			// GraticulePoser
			float multiplier = VFInput.shift ? 10f : 1.0f; // shift: x10
			xWanted += VFInput._moveRight.value * 0.3f * moveSens * GameCamera.camRotSensX * multiplier;
			xWanted += VFInput._moveLeft.value * -0.3f * moveSens * GameCamera.camRotSensX * multiplier;
			yWanted += VFInput._moveForward.value * 0.3f * moveSens * GameCamera.camRotSensY * multiplier;
			yWanted += VFInput._moveBackward.value * -0.3f * moveSens * GameCamera.camRotSensY * multiplier;
			zWanted += (VFInput._cameraZoomIn + VFInput._cameraZoomOut) * GameCamera.camZoomSens * 100f * moveSens * multiplier;
			float xDelta = Lerp.Tween(0, xWanted, damp * 100f);
			float yDelta = Lerp.Tween(0, yWanted, damp * 100f);
			float zDelta = Lerp.Tween(0, zWanted, damp * 100f);
			xWanted -= xDelta;
			yWanted -= yDelta;
			zWanted -= zDelta;

			cameraPose.rotation *= Quaternion.Euler(pitch, yaw, roll);
			cameraPose.position += cameraPose.rotation * Vector3.right * xDelta;
			cameraPose.position += cameraPose.rotation * Vector3.up * yDelta;
			cameraPose.position += cameraPose.rotation * Vector3.forward * zDelta;
		}
	}
}
