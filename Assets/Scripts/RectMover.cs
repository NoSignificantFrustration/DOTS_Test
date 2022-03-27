using Unity.Burst;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

public class RectMover : MonoBehaviour
{

    [SerializeField] private GameObject prefab;
    [SerializeField] private int amount;
    [SerializeField] private float boundRadius;
    [SerializeField] private float speed;
    
    private int[] directionArray;
    private Transform[] transforms;

    private void Awake()
    {
        directionArray = new int[amount];
        transforms = new Transform[amount];
    }

    // Start is called before the first frame update
    void Start()
    {

        for (int i = 0; i < amount; i++)
        {
            transforms[i] = GameObject.Instantiate(prefab, new Vector3(UnityEngine.Random.Range(-boundRadius + 0.01f, boundRadius), UnityEngine.Random.Range(-boundRadius + 0.01f, boundRadius), 0f), new Quaternion()).transform;
            directionArray[i] = UnityEngine.Random.Range(0, 2);

        }
    }

    // Update is called once per frame
    void Update()
    {
        //float time = Time.realtimeSinceStartup;

        MoveJob job = new MoveJob();

        job.speed = speed;
        job.boundRadius = boundRadius;
        job.deltaTime = Time.deltaTime;

        job.posArray = new NativeArray<float>(amount, Allocator.TempJob);
        job.directionArray = new NativeArray<int>(directionArray, Allocator.TempJob);

        job.testArray = new NativeArray<UnsafeList<int>>(1, Allocator.TempJob);
        job.testArray[0] = new UnsafeList<int>();

        for (int i = 0; i < amount; i++)
        {
            job.posArray[i] = transforms[i].position.y;
        }

        //Debug.Log("Allocation" + (Time.realtimeSinceStartup - time));
        //time = Time.realtimeSinceStartup;

        JobHandle handle = job.Schedule(amount, 64);

        handle.Complete();

        for (int i = 0; i < amount; i++)
        {
            transforms[i].position = new Vector3(transforms[i].position.x, job.posArray[i], 0f);
        }

        directionArray = job.directionArray.ToArray();

        job.posArray.Dispose();
        job.directionArray.Dispose();
        job.testArray.Dispose();

        //Debug.Log("Deallocation" + (Time.realtimeSinceStartup - time));
    }

    [BurstCompile]
    public struct MoveJob : IJobParallelFor
    {

        public float speed;
        public float boundRadius;
        public float deltaTime;

        public NativeArray<float> posArray;
        public NativeArray<int> directionArray;
        public NativeArray<UnsafeList<int>> testArray;

        public void Execute(int i)
        {

            int direction;
            if (directionArray[i] == 1)
            {
                direction = 1;
            }
            else
            {
                direction = -1;
            }

            float newPos = posArray[i] + direction * speed * deltaTime;

            if (Mathf.Abs(newPos) > boundRadius)
            {
                float deviaton = (newPos - direction * boundRadius);

                if (Mathf.Abs(deviaton) > boundRadius)
                {
                    deviaton = direction * boundRadius;
                }

                newPos = direction * boundRadius + deviaton;
                //newPos = direction * boundRadius;
                if (directionArray[i] == 1)
                {
                    directionArray[i] = 0;
                }
                else
                {
                    directionArray[i] = 1;
                }
            }

            posArray[i] = newPos;


        }
    }

    //This does NOT work for putting into a native array
    public struct TestStruct
    {
        public NativeList<int> testList;
    }
}
