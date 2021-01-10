using System;
using System.Data;
using System.Threading.Tasks;
using dftbsvc.Models;

namespace dftbsvc.Services
{
    public interface ICommand<T>
    {
        Task ExecuteAsync(T item, Action<T, bool> postAction);
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
        public abstract Task ExecuteAsync(T item, Action<T, bool> postAction);
    }

    public class ItemCommand
        : Command<ItemEvent>
    {
        public ItemCommand(IItemRepository repository)
            : base(repository) 
        { }

        public override async Task ExecuteAsync(ItemEvent item, Action<ItemEvent, bool> postAction)
        {
            switch (item.Operation)
            {
                case Operation.Create:
                    await Repository.CreateItemAsync(item, postAction).ConfigureAwait(false);
                    break;
                case Operation.Delete:
                    await Repository.DeleteItemAsync(item, postAction).ConfigureAwait(false);
                    break;
                case Operation.Update:
                    await Repository.UpdateItemAsync(item, postAction).ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    public class ItemTemplateCommand
        : Command<ItemTemplateEvent>
    {
        public ItemTemplateCommand(IItemRepository repository)
            : base(repository)
        { }

        public override async Task ExecuteAsync(ItemTemplateEvent item, Action<ItemTemplateEvent, bool> postAction)
        {
            switch (item.Operation)
            {
                case Operation.Create:
                    await Repository.CreateItemTemplateAsync(item, postAction).ConfigureAwait(false);
                    break;
                case Operation.Delete:
                    await Repository.DeleteItemTemplateAsync(item, postAction).ConfigureAwait(false);
                    break;
                case Operation.Update:
                    await Repository.UpdateItemTemplateAsync(item, postAction).ConfigureAwait(false);
                    break;
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