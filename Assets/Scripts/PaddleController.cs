using UnityEngine;

namespace Assets
{
    public class PaddleController : MonoBehaviour
    {

        public bool isPaddle1;
        public float speed = 5f;
        
        void Start () {
		
        }
	
        // Update is called once per frame
        void Update ()
        {
            transform.position = new Vector3(transform.position.x,0,Input.GetAxis(isPaddle1?"Vertical":"Vertical2") * ScoreControl.Instance.ArenaWidth);
            if(isPaddle1)
                transform.Translate(0f, Input.GetAxis("Vertical")* speed * Time.deltaTime, 0f);
            else
                transform.Translate(0f, Input.GetAxis("Vertical2") * speed * Time.deltaTime, 0f);
        }
    }
}
