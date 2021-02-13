using System;
using System.Data;
using System.Threading.Tasks;
using dftbsvc.Models;

namespace dftbsvc.Services
{
    public interface ICommand<T>
    {
        Task<bool> ExecuteAsync(T item);
    }

    public interface ICommandGenerator
    {
        ICommand<T> GenerateCommand<T>(IItemRepository repository, T item);
    }

    public abstract class Command<T> 
        : ICommand<T>
    {
        readonly IItemRepository _repository;
        public Command(IItemRepository repository)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }
        protected IItemRepository Repository => _repository;
        public abstract Task<bool> ExecuteAsync(T item);
    }

    public class ItemCommand
        : MapCommand<ItemEvent>
    {
        public ItemCommand(IItemRepository repository)
            : base(
                (item) => repository.CreateItemAsync(item),
                (item) => repository.UpdateItemAsync(item),
                (item) => repository.DeleteItemAsync(item)
            )
        { }
    }

    public class ItemTemplateCommand
        : MapCommand<ItemTemplateEvent>
    {
        public ItemTemplateCommand(IItemRepository repository)
            : base(
                (itemTemplate) => repository.CreateItemTemplateAsync(itemTemplate),
                (itemTemplate) => repository.UpdateItemTemplateAsync(itemTemplate),
                (itemTemplate) => repository.DeleteItemTemplateAsync(itemTemplate)
            )
        { }
    }

    public class MapCommand<T>
        : ICommand<T> where T : Event
    {
        readonly Func<T, Task<bool>>[] commands = new Func<T, Task<bool>>[3];

        public MapCommand(Func<T, Task<bool>> create, Func<T, Task<bool>> update, Func<T, Task<bool>> delete)
        {
            commands[0] = create;
            commands[1] = update;
            commands[2] = delete;
        }

        public async Task<bool> ExecuteAsync(T item)
        {            
            switch (item.Operation)
            {
                case Operation.Create:
                    return await commands[0](item).ConfigureAwait(false);
                case Operation.Update:
                    return await commands[1](item).ConfigureAwait(false);
                case Operation.Delete:
                    return await commands[2](item).ConfigureAwait(false);
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    public class CommandGenerator
        : ICommandGenerator
    {
        public ICommand<T> GenerateCommand<T>(IItemRepository repository, T item)
        {
            switch (item)
            {
                case ItemEvent t1:
                    return (ICommand<T>)new ItemCommand(repository);
                case ItemTemplateEvent t2:
                    return (ICommand<T>)new ItemTemplateCommand(repository);
                default:
                    throw new NotSupportedException();
            }
        }
    }
}