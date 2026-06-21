using LitMotion;
using LitMotion.Extensions;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerKeyBoardInput : MonoBehaviour
{
    [SerializeField] private Image imageX;
    [SerializeField] private Image imageC;
    [SerializeField] private Image imageN;
    [SerializeField] private Image imageM;
    [SerializeField] private CatController catController;

    [SerializeField] private float _turnDuration = 0.1f;
    private PlayerInput _playerInput;
    private void Start()
    {
        _playerInput = new PlayerInput();
        _playerInput.Player.Enable();

        _playerInput.Player.Lane1.performed += HandleLane01;
        _playerInput.Player.Lane2.performed += HandleLane02;
        _playerInput.Player.Lane3.performed += HandleLane03;
        _playerInput.Player.Lane4.performed += HandleLane04;
    }

    private void HandleLane01(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageX.rectTransform);
            catController.HandleTurn(false, false, _turnDuration).Forget();
        }
    }
    private void HandleLane02(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageC.rectTransform);
            catController.HandleTurn(true, false, _turnDuration).Forget();
        }
    }
    private void HandleLane03(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageN.rectTransform);
            catController.HandleTurn(false, true, _turnDuration).Forget();
        }
    }
    private void HandleLane04(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            HandleMovement(imageM.rectTransform);
            catController.HandleTurn(true, true, _turnDuration).Forget();
        }
    }

    private void HandleMovement(RectTransform tran)
    {
        LMotion.Create(1.0f, 1.5f, 0.1f)
            .WithOnComplete(() =>
            {
                LMotion.Create(1.5f, 1.0f, 0.1f)
                    .BindToLocalScaleXYZ(tran)
                    .AddTo(this);
            })
            .BindToLocalScaleXYZ(tran)
            .AddTo(this);
    }
}
