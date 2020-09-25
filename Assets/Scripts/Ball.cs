using System.Collections;
using UnityEngine;

namespace Assets
{
    public class Ball : MonoBehaviour
    {

        public float speed = 5f;

        void Start()
        {
            StartCoroutine(Pause());
        }

        void LaunchBall()
        {
            transform.position= Vector3.zero;

            float sx = Random.Range(0, 2) == 0 ? -1 : 1;
            float sy = Random.Range(0, 2) == 0 ? -1 : 1;

            GetComponent<Rigidbody>().velocity = new Vector3(speed * sx, 0f, speed * sy);
        }
        
        IEnumerator Pause()
        {

            yield return new WaitForSeconds(2.5f);
            LaunchBall();
        }

        // Update is called once per frame
        void Update()
        {
            
            if (Camera.main.WorldToViewportPoint(transform.position).x>1)
            {
                ScoreControl.Instance.Player1++;

                StartCoroutine(Pause());
            }
            else if (Camera.main.WorldToViewportPoint(transform.position).x<0)
            {
                ScoreControl.Instance.Player2++;
                
                StartCoroutine(Pause());
            }






        }
    }
}
