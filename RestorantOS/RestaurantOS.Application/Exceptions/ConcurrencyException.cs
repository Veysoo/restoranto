namespace RestaurantOS.Application.Exceptions;

public class ConcurrencyException : Exception
{
    public ConcurrencyException() : base("Kayıt başka bir kullanıcı tarafından güncellendi. Lütfen yenileyin.") { }
}
