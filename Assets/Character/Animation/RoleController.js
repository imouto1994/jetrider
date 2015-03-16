#pragma strict
var controller:Animator;
function Start () {
}

function Update () {
	if (Input.GetKey(KeyCode.UpArrow)){
		controller.SetBool("Up",true);
	}else{
		controller.SetBool("Up",false);
	}
			
}