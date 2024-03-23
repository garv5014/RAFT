namespace Raft_Library.Shop.shared;

public class Saga
{
    private readonly List<Func<Task>> _operations = new List<Func<Task>>();
    private readonly List<Func<Task>> _compensations = new List<Func<Task>>();
    private bool _compensationRequired = false;

    public void AddStep(Func<Task> operation, Func<Task> compensation)
    {
        _operations.Add(operation);
        _compensations.Add(compensation);
    }

    public async Task ExecuteAsync()
    {
        for (int i = 0; i < _operations.Count; i++)
        {
            try
            {
                await _operations[i]();
                if (_compensationRequired)
                {
                    await Rollback(i - 1);
                    return;
                }
            }
            catch
            {
                _compensationRequired = true;
                await Rollback(i);
                break;
            }
        }
    }

    private async Task Rollback(int fromStep)
    {
        Console.WriteLine("Rolling back...");
        for (int i = fromStep; i >= 0; i--)
        {
            try
            {
                await _compensations[i]();
            }
            catch { }
        }
    }
}
