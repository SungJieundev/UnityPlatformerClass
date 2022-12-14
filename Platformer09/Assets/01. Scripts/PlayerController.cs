using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using GlobalType;

public class PlayerController : MonoBehaviour
{
    #region public properties
    [Header("Player Properties")]
    public float walkSpeed = 10f;
    public float creepSpeed = 5f;
    public float gravity = 20f;
    public float jumpSpeed = 15f;
    public float doubleJumpSpeed = 10f;
    public float xWallJumpSpeed = 15f;
    public float yWallJumpSpeed = 15f;
    public float wallRunSpeed = 8f;
    public float wallSlideAmount = 0.1f;
    public float dashSpeed = 40f;
    public float dashTime = 0.2f;
    public float dashCoolDownTime = 1f;

    // player ability toggle;
    [Header("Player Abilities")]
    public bool canDoubleJump;
    public bool canTripleJump;
    public bool canWallJump;
    public bool canWallRun;
    public bool canWallSlide;
    public bool canAirDash;
    public bool canGroundDash;

    // Player state
    [Header("Player States")]
    public bool isJumping;
    public bool isDoubleJumping;
    public bool isTripleJumping;
    public bool isWallJumping;
    public bool isWallRunning;
    public bool isWallSliding;
    public bool isDucking;
    public bool isCreeping;
    public bool isDashing;

    #endregion

    #region private properties
    // input flags
    private bool _startJump;
    private bool _releaseJump;

    private Vector2 _input;
    private Vector2 _moveDir;
    private CharacterController2D _characterController;
    private CapsuleCollider2D _capsuleCollider;
    private SpriteRenderer _spriteRenderer;

    private Vector2 _originalColliderSize;
    private bool _ableToWallRun;
    private bool _facingRight;
    private float _dashTimer = 0f;
    #endregion

    private void Awake()
    {
        _characterController = GetComponent<CharacterController2D>();
        _capsuleCollider = GetComponent<CapsuleCollider2D>();
        _originalColliderSize = _capsuleCollider.size;
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _dashTimer += Time.deltaTime;

        if (!isWallJumping)
        {

            if (_moveDir.x > 0f)
            {
                transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                _facingRight = true;
            }
            else if (_moveDir.x < 0f)
            {
                transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                _facingRight = false;
            }

            if (isDashing)
            {
                if (_facingRight)
                {
                    _moveDir.x = dashSpeed;
                }
                else
                {
                    _moveDir.x = -dashSpeed;
                }
                _moveDir.y = 0f;
            }
            else if( isCreeping){
                _moveDir.x = _input.x * creepSpeed;
                
            }
            else{
                _moveDir.x = _input.x * walkSpeed;
            }
        }

        if (_characterController.below)
        { // on the ground

            _moveDir.y = 0f;
            isJumping = false;
            isDoubleJumping = false;
            isTripleJumping = false;
            isWallJumping = false;
            isWallRunning = false;

            if (_startJump)
            {
                _startJump = false;

                if(isDucking && _characterController.groundType == GroundType.OneWayPlatform){
                    StartCoroutine(DisableOneWayPlatform(true));
                }
                else{

                    _moveDir.y = jumpSpeed;
                }
                
                isJumping = true;
                _ableToWallRun = true;
                _characterController.DisableGroundCheck(0.1f);
                
            }

            //ducking, creeping
            if (_input.y < 0f)
            {
                if (!isDucking && !isCreeping)
                {
                    isDucking = true;

                    // ???????????? ????????? ????????? ?????????
                    _capsuleCollider.size = new Vector2(_capsuleCollider.size.x, _capsuleCollider.size.y / 2);
                    // position.y ??????
                    transform.position = new Vector2(transform.position.x, transform.position.y - (_originalColliderSize.y / 4));
                    // Sprite ??????
                    _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp_crouching");
                }
            }
            else
            {
                if (isDucking || isCreeping)
                {

                    RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, transform.localScale,
                    CapsuleDirection2D.Vertical, 0f, Vector2.up, _originalColliderSize.y / 2, _characterController.layerMask);

                    if (!hitCeiling.collider)
                    {

                        isDucking = false;
                        isCreeping = false;

                        _capsuleCollider.size = _originalColliderSize;
                        transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
                        _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
                    }
                }
            }

            // creeping
            if (isDucking && _moveDir.x != 0)
            {
                isCreeping = true;
            }
            else
            {
                isCreeping = false;
            }
        }
        else
        { // ?????????

            if ((isCreeping || isDucking) && _moveDir.y > 0)
            {
                StartCoroutine(ClearDuckingState());
            }

            if (_releaseJump)
            {
                _releaseJump = false;

                if (_moveDir.y > 0)
                {
                    _moveDir.y *= 0.5f;
                }
            }

            // pressed jump button in air
            if (_startJump)
            {

                // triple jumping
                if (canTripleJump && (!_characterController.left && !_characterController.right))
                {
                    if (isDoubleJumping && !isTripleJumping)
                    {
                        _moveDir.y = doubleJumpSpeed;
                        isTripleJumping = true;
                    }
                }

                // double jumping
                if (canDoubleJump && (!_characterController.left && !_characterController.right))
                {
                    if (!isDoubleJumping)
                    {
                        _moveDir.y = doubleJumpSpeed;
                        isDoubleJumping = true;
                    }
                }

                if (canWallJump && (_characterController.left || _characterController.right))
                {
                    if (_characterController.left)
                    {
                        _moveDir.x = xWallJumpSpeed;
                        _moveDir.y = yWallJumpSpeed;
                        transform.rotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    else if (_characterController.right)
                    {
                        _moveDir.x = -xWallJumpSpeed;
                        _moveDir.y = yWallJumpSpeed;
                        transform.rotation = Quaternion.Euler(0f, 180f, 0f);
                    }
                    isWallJumping = true;
                    StartCoroutine(WallJumpWaiter());
                }

                _startJump = false;
            }

            // wall running
            if (canWallRun && _characterController.left || _characterController.right)
            {

                if (_input.y > 0f && _ableToWallRun)
                {
                    _moveDir.y = wallRunSpeed;
                }
                //isWallRunning = true;

                StartCoroutine(WallRunWaiter());
            }

            GravityCalculation();
        }
        _characterController.Move(_moveDir * Time.deltaTime);
    }

    void GravityCalculation()
    {
        if (_moveDir.y > 0f && _characterController.above)
        {   
            if(_characterController.ceilingType == GlobalType.GroundType.OneWayPlatform){
                
                StartCoroutine(DisableOneWayPlatform(false));
            }
            else{

                _moveDir.y = 0f;
            }
        }

        if (canWallSlide && _characterController.left || _characterController.right)
        {
            if (_moveDir.y <= 0)
            {
                _moveDir.y -= gravity * wallSlideAmount * Time.deltaTime;
            }
            else
            {
                _moveDir.y -= gravity * Time.deltaTime;
            }
        }
        else
        {
            _moveDir.y -= gravity * Time.deltaTime;
        }
    }

    #region  Input Methods
    public void OnMovement(InputAction.CallbackContext context)
    {
        _input = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            _startJump = true;
            _releaseJump = false;
        }
        else if (context.canceled)
        {
            _startJump = false;
            _releaseJump = true;
        }
    }


    public void OnDash(InputAction.CallbackContext context)
    {
        if (context.started && _dashTimer >= dashCoolDownTime)
        {
            if ((canAirDash && !_characterController.below) || (canGroundDash && _characterController.below))
            {
                StartCoroutine(Dash());
            }
        }
    }
#endregion 
    
    #region coroutines
    IEnumerator Dash()
    {
        isDashing = true;
        yield return new WaitForSeconds(dashTime);
        isDashing = false;
        _dashTimer = 0f;
    }

    IEnumerator WallJumpWaiter()
    {
        isWallJumping = true;
        yield return new WaitForSeconds(0.4f);
        isWallJumping = false;
    }

    IEnumerator WallRunWaiter()
    {
        isWallRunning = true;
        yield return new WaitForSeconds(0.5f);
        isWallRunning = false;
        _ableToWallRun = false;
    }

    IEnumerator ClearDuckingState()
    {
        yield return new WaitForSeconds(0.05f);

        RaycastHit2D hitCeiling = Physics2D.CapsuleCast(_capsuleCollider.bounds.center, transform.localScale,
                    CapsuleDirection2D.Vertical, 0f, Vector2.up, _originalColliderSize.y / 2, _characterController.layerMask);

        if (!hitCeiling.collider)
        {

            isDucking = false;
            isCreeping = false;

            _capsuleCollider.size = _originalColliderSize;
            transform.position = new Vector2(transform.position.x, transform.position.y + (_originalColliderSize.y / 4));
            _spriteRenderer.sprite = Resources.Load<Sprite>("directionSpriteUp");
        }
    }

    IEnumerator DisableOneWayPlatform(bool checkBelow){
        
        GameObject tempOneWayPlatform = null;

        if(checkBelow){
            Vector2 raycastBelow = transform.position - new Vector3(0, _capsuleCollider.size.y * 0.5f, 0);
            RaycastHit2D hit = Physics2D.Raycast(raycastBelow, Vector2.down, 0.2f, _characterController.layerMask);

            if(hit.collider){
                tempOneWayPlatform = hit.collider.gameObject;
            }
        }
        else{

            Vector2 raycastAbove = transform.position + new Vector3(0, _capsuleCollider.size.y * 0.5f, 0);
            RaycastHit2D hit = Physics2D.Raycast(raycastAbove, Vector2.up, 0.4f, _characterController.layerMask);

            if(hit.collider){
                tempOneWayPlatform = hit.collider.gameObject;
            }
        }


        if(tempOneWayPlatform){
            tempOneWayPlatform.GetComponent<EdgeCollider2D>().enabled = false;
        }

        yield return new WaitForSeconds(0.4f);

        if(tempOneWayPlatform){
            tempOneWayPlatform.GetComponent<EdgeCollider2D>().enabled = true;
        }

    }
#endregion
}