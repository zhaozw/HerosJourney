using UnityEngine;
using System.Collections;

public class PlayerControl : MonoBehaviour 
{
	#region Variables / Properties
	
	public AudioClip attackSound;
	
	public bool DebugMode = false;
	public float DeadZoneRange = 0.2f;
	public bool canOverthrust = false;
	public bool canUnderthrust = false;
	public bool allowAction = true;
	public bool isCrouching = false;
	public bool isJumping = false;
	public bool isAttacking = false;
	
	public string idleLeft;
	public string idleRight;
	public string hitLeft;
	public string hitRight;
	public string walkLeft;
	public string walkRight;
	public string jumpLeft;
	public string jumpRight;
	public string crouchLeft;
	public string crouchRight;
	public string attackLeft;
	public string attackRight;
	public string crouchAttackLeft;
	public string crouchAttackRight;
	public string jumpAttackLeft;
	public string jumpAttackRight;
	public string underCut;
	public string overCut;
	
	private SidescrollingMovement _movement;
	private HitboxController _hitboxController;
	
	private float _verticalInput;
	private float _horizontalInput;
	private bool _isMoving = false;
	private bool _isFalling = false;
	private bool _facingRight = true;	
	private string _currentSequence;
	private SpriteSystem _animation;
	
	#endregion Variables / Properties
	
	#region Engine Hooks
	
	public void Start() 
	{
		_movement = gameObject.GetComponent<SidescrollingMovement>();
		_animation = gameObject.GetComponentInChildren<SpriteSystem>();
		_hitboxController = gameObject.GetComponentInChildren<HitboxController>();
	}
	
	public void Update()
	{	   	
		CollectInput();
		
		CheckHorizontal();
		CheckCrouching();
		CheckOverthrust();
		CheckUnderthrust();
		
		MoveCharacter();
		PerformJump();
		
		CheckAttack();
		CheckInjured();
		
		PerformAnimation(_currentSequence);
		PerformHitboxAnimation(_currentSequence);
	}
	
	#endregion Engine Hooks
	
	#region Methods
	
	public void Halt()
	{
		allowAction = false;
		isAttacking = false;
		isCrouching = false;
		_verticalInput = 0;
		_horizontalInput = 0;
		
		_currentSequence = _facingRight ? idleRight : idleLeft;
	}
	
	public void Resume()
	{
		allowAction = true;
	}
	
	private void CollectInput()
	{
		if(! allowAction)
		{
			if(DebugMode)
				Debug.LogWarning("Input has been suspended!  Call Resume() to give control back to the player.");
			
			isAttacking = false;
			_verticalInput = 0;
			_horizontalInput = 0;
			return;
		}
		
		isAttacking = Input.GetButtonUp("Fire1");
		_verticalInput = Input.GetAxisRaw("Vertical");
		_horizontalInput = Input.GetAxisRaw("Horizontal");
	}
	
	private bool InputIsDead(float input)
	{
		return input > -DeadZoneRange
			   && input < DeadZoneRange;
	}
	
	private bool InputIsPositive(float input)
	{
		return input > DeadZoneRange;
	}
	
	private bool InputIsNegative(float input)
	{
		return input < DeadZoneRange;
	}
	
	private void CheckAttack()
	{				
		if(! isAttacking)
			return;
		
		if(DebugMode)
			Debug.Log("The player released the attack button, so attacking!");
		
		_currentSequence = (isCrouching || isJumping)
						   ? _facingRight ? crouchAttackRight : crouchAttackLeft
				           : _facingRight ? attackRight : attackLeft;
	}
	
	private void CheckHorizontal()
	{
		if(! InputIsDead(_horizontalInput))
			_facingRight = InputIsPositive(_horizontalInput);
		
		_currentSequence = (InputIsDead(_horizontalInput))
						   ? _facingRight ? idleRight : idleLeft
						   : _facingRight ? walkRight : walkLeft;
		_isMoving = (! InputIsDead(_horizontalInput));
	}
	
	private void CheckCrouching()
	{	
		if(isJumping
		   || InputIsDead(_verticalInput)
		   || InputIsPositive(_verticalInput))
		{
			isCrouching = false;
			return;
		}
		
		isCrouching = true;
		_currentSequence = _facingRight ? crouchRight : crouchLeft;
		_isMoving = false;
	}
	
	private void CheckOverthrust()
	{
		if(! canOverthrust)
			return;
		
		if(InputIsDead(_verticalInput)
		   || InputIsNegative(_verticalInput))
			return;
		
		if(isJumping)
			_currentSequence = overCut;
	}
	
	private void CheckUnderthrust()
	{
		if(! canUnderthrust)
			return;
		
		if(InputIsDead(_verticalInput)
		   || InputIsPositive(_verticalInput))
			return;
		
		if(isJumping)
			_currentSequence = underCut;
	}
	
	private void CheckInjured()
	{
		if(! _movement.isHit)
			return;
		
		if(DebugMode)
			Debug.Log("Have been injured!  Playing the injured animation.");
		
		// Override all flags when hit...
		_isMoving = false;
		_isFalling = false;
		isAttacking = false;
		isCrouching = false;
		isJumping = false;
		
		_currentSequence = _facingRight ? hitRight : hitLeft;
	}
	
	private void PerformJump()
	{		
		if(isCrouching)
			return;
		
		bool jumpPressed = Input.GetButtonDown("Jump"); 
		bool jumpReleased = Input.GetButtonUp("Jump");
			
		if(jumpPressed
		   && _movement.isGrounded
		   && ! _isFalling)
			_movement.PartialJump();
		
		if(jumpReleased
		   && ! _isFalling)
		{
			_movement.SlowJump();
			_isFalling = true;
		}
		
		if(_movement.HitHead)
		{
			_movement.SlowJump();
			_isFalling = true;
		}
		
		if(_movement.isGrounded
		   && _isFalling)
			_isFalling = false;
		
		isJumping = ! _movement.isGrounded;
		
		if(! _movement.isGrounded)
			_currentSequence = _facingRight ? crouchRight : crouchLeft;
	}
	
	private void MoveCharacter()
	{		
		if(! _isMoving)
		{
			_movement.ClearMovement();
			return;
		}
		
		if(_facingRight)
			_movement.MoveRight();
		else
			_movement.MoveLeft();
	}
	
	private void PerformHitboxAnimation(string animation)
	{
		AnimationMode mode = isAttacking 
			? AnimationMode.OneShot 
			: AnimationMode.Loop;
		
		bool animationLocked = isAttacking || _movement.isHit;
		
		_hitboxController.PlaySingleFrame(animation, animationLocked, mode);
	}
	
	private void PerformAnimation(string animation)
	{
		AnimationMode mode = isAttacking 
			? AnimationMode.OneShot 
			: AnimationMode.Loop;
		
		bool animationLocked = isAttacking || _movement.isHit;
		
		_animation.PlaySingleFrame(animation, animationLocked, mode);
	}
	
	#endregion Methods
}
