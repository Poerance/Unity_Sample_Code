// Used for click and drag main player

namespace Pointer_Input_System {
    using UnityEngine;


    public class Circle : MonoBehaviour {
        private Player_Input playerInput;
        private bool isPressed;
        private bool isTouched;
        private CircleCollider2D myCollider; // Or any collider type
        private Vector2 pointerPosition;

        private void Awake() {
            myCollider = GetComponent<CircleCollider2D>();
        }

        private void OnEnable() {
            playerInput = new Player_Input();
            playerInput.Player.Enable();

            playerInput.Player.Pointer_State.performed += OnPointerPerformed;
            playerInput.Player.Pointer_State.canceled += OnPointerCanceled;
        }

        private void OnDisable() {
            playerInput.Player.Pointer_State.performed -= OnPointerPerformed;
            playerInput.Player.Pointer_State.canceled -= OnPointerCanceled;

            playerInput.Player.Disable();

        }

        private Vector2 GetPointerPosition() {
            Vector2 position = Camera.main.ScreenToWorldPoint(playerInput.Player.Pointer_Position.ReadValue<Vector2>());
            return position;
        }

        private void OnPointerPerformed(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
            isPressed = true;
            Debug.Log($"Is Pressed ? {isPressed}");
        }

        private void OnPointerCanceled(UnityEngine.InputSystem.InputAction.CallbackContext obj) {
            isPressed = false;
            isTouched = false;
            Debug.Log($"Is Pressed ? {isPressed}");
        }

        private void Update() {
            if (!isPressed) return;

            pointerPosition = GetPointerPosition();
            if (!isTouched) isTouched = myCollider.OverlapPoint(pointerPosition); // Only check when Mouse is not pressed (OnPointerCanceled has isTouched = false)
            
            if (!isTouched) return; // Basically it's double checking

            transform.position = pointerPosition;
        }
    }
}
