using System;
using UnityEngine;

public class InputManager : MonoBehaviour
{
    private InputSystem_Actions playerInput;

    private Vector2 moveInput;
    public Action<Vector2> OnMoveContinues;

    void Start()
    {
        playerInput = new();

        playerInput.Player.Move.performed += ctr => moveInput = ctr.ReadValue<Vector2>();
        playerInput.Player.Move.canceled += ctr => moveInput = Vector2.zero;
        playerInput.Enable();
    }

    void Update()
    {
        MoveContinues();
    }

    private void MoveContinues()
    {
        OnMoveContinues?.Invoke(moveInput);
    }

    void OnDestroy()
    {
        playerInput.Disable();
    }
}
