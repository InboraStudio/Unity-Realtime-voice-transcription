using UnityEngine;

namespace LeastSquares.Undertone
{
    public class RotateBackgroundUndertone : MonoBehaviour
    {
        [SerializeField] public float speed = 10f;

        void Update()
        {
            transform.Rotate(Vector3.forward * speed * Time.deltaTime);
        }
    }
}