namespace Frontend.Services
{
    public class AuthState
    {
        public event Func<Task>? OnChange;

        public async void NotifyAuthenticationChanged()
        {
            if (OnChange is not null)
                await OnChange.Invoke();
        }
    }
}
