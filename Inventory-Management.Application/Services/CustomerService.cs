using Inventory_Management.Application.DTOs.Customer;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(IGenericRepository<Customer> customerRepository, IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDto> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");
        return MapToDto(customer);
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Select(MapToDto);
    }

    public async Task<CustomerDto> AddAsync(CreateCustomerDto dto)
    {
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            Address = dto.Address,
            CreatedAt = DateTime.UtcNow
        };
        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(customer);
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Customer with ID {dto.Id} not found.");

        customer.Name = dto.Name;
        customer.Email = dto.Email;
        customer.PhoneNumber = dto.PhoneNumber;
        customer.Address = dto.Address;

        await _customerRepository.UpdateAsync(customer);
        await _unitOfWork.SaveChangesAsync();
        return MapToDto(customer);
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        await _customerRepository.DeleteAsync(customer.Id);
        await _unitOfWork.SaveChangesAsync();
    }

    private static CustomerDto MapToDto(Customer c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Email = c.Email,
        PhoneNumber = c.PhoneNumber,
        Address = c.Address,
        CreatedAt = c.CreatedAt
    };
}
