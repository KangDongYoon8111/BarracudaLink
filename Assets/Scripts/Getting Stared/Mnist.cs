using UnityEngine;
using Unity.Barracuda;

public class Mnist : MonoBehaviour
{
    private enum Type { CPU, GPU }
    [Header("�߷� ���� ����")]
    [SerializeField] private Type type;

    [Header("AI Model ���õ�����")]
    public NNModel modelAsset;

    private Model m_RuntimeModel;
    private IWorker worker;

    void Start()
    {
        m_RuntimeModel = ModelLoader.Load(modelAsset);

        switch (type)
        {
            case Type.CPU:
                worker = WorkerFactory.CreateWorker(WorkerFactory.Type.ComputePrecompiled, m_RuntimeModel);
                break;
            case Type.GPU:
                worker = WorkerFactory.CreateWorker(WorkerFactory.Type.CSharpBurst, m_RuntimeModel);
                break;
            default:
                Debug.Log("There is a problem with the inference engine load.");
                break;
        }

        RunModel();
    }

    private void RunModel()
    {
        //Tensor input = new Tensor(batch, height, width, channels);
        //worker.Execute(input);
        //Tensor O = worker.PeekOutput("output_layer_name");
        //input.Dispose();
    }
}
