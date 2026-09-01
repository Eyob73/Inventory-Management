using Inventory_Management.Application.DTOs.Customer;
using Inventory_Management.Application.Interfaces.Repositories;
using Inventory_Management.Application.Interfaces.Services;
using Inventory_Management.Domain.Entities;

namespace Inventory_Management.Application.Services;

public class CustomerService : ICustomerService
{
    private readonly IGenericRepository<Customer> _customerRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerService(
        IGenericRepository<Customer> customerRepository,
        ISaleRepository saleRepository,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CustomerDetailDto> GetByIdAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        var allSales = await _saleRepository.GetAllAsync();
        var customerSales = allSales
            .Where(s => s.CustomerId == id && s.Status != "Cancelled")
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        var salesHistory = new List<CustomerSaleDto>();
        foreach (var sale in customerSales)
        {
            var fullSale = await _saleRepository.GetWithItemsByIdAsync(sale.Id) ?? sale;
            salesHistory.Add(new CustomerSaleDto
            {
                Id = fullSale.Id,
                SaleNumber = fullSale.SaleNumber,
                CreatedAt = fullSale.CreatedAt,
                TotalAmount = fullSale.TotalAmount,
                PaymentMethod = fullSale.PaymentMethod,
                ItemsCount = fullSale.SaleItems.Count,
                Status = fullSale.Status ?? "Completed",
                Items = fullSale.SaleItems.Select(item => new CustomerSaleItemDto
                {
                    ProductName = item.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice,
                    TotalPrice = item.TotalPrice
                }).ToList()
            });
        }

        return new CustomerDetailDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            TotalSalesCount = salesHistory.Count,
            TotalSpent = salesHistory.Sum(s => s.TotalAmount),
            SalesHistory = salesHistory
        };
    }

    public async Task<IEnumerable<CustomerDto>> GetAllAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        var allSales = await _saleRepository.GetAllAsync();

        return customers.Select(c =>
        {
            var cSales = allSales.Where(s => s.CustomerId == c.Id && s.Status != "Cancelled").ToList();
            return new CustomerDto
            {
                Id = c.Id,
                Name = c.Name,
                Email = c.Email,
                PhoneNumber = c.PhoneNumber,
                Address = c.Address,
                IsActive = c.IsActive,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                TotalSalesCount = cSales.Count,
                TotalSpent = cSales.Sum(s => s.TotalAmount)
            };
        });
    }

    public async Task<CustomerDto> AddAsync(CreateCustomerDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            throw new ArgumentException("Customer phone number is required.");

        var existingCustomers = await _customerRepository.GetAllAsync();
        
        var trimmedPhone = dto.PhoneNumber.Trim();
        if (existingCustomers.Any(c => !c.IsDeleted && c.PhoneNumber.Trim().Equals(trimmedPhone, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"A customer with phone number '{trimmedPhone}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var trimmedEmail = dto.Email.Trim();
            if (existingCustomers.Any(c => !c.IsDeleted && !string.IsNullOrEmpty(c.Email) && c.Email.Trim().Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"A customer with email address '{trimmedEmail}' already exists.");
            }
        }

        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            Name = dto.Name.Trim(),
            Email = dto.Email?.Trim() ?? string.Empty,
            PhoneNumber = dto.PhoneNumber.Trim(),
            Address = dto.Address?.Trim() ?? string.Empty,
            IsActive = dto.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        await _customerRepository.AddAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            TotalSalesCount = 0,
            TotalSpent = 0
        };
    }

    public async Task<CustomerDto> UpdateAsync(UpdateCustomerDto dto)
    {
        var customer = await _customerRepository.GetByIdAsync(dto.Id)
            ?? throw new KeyNotFoundException($"Customer with ID {dto.Id} not found.");

        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new ArgumentException("Customer name is required.");

        if (string.IsNullOrWhiteSpace(dto.PhoneNumber))
            throw new ArgumentException("Customer phone number is required.");

        var existingCustomers = await _customerRepository.GetAllAsync();

        var trimmedPhone = dto.PhoneNumber.Trim();
        if (existingCustomers.Any(c => c.Id != dto.Id && !c.IsDeleted && c.PhoneNumber.Trim().Equals(trimmedPhone, StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException($"Another customer with phone number '{trimmedPhone}' already exists.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var trimmedEmail = dto.Email.Trim();
            if (existingCustomers.Any(c => c.Id != dto.Id && !c.IsDeleted && !string.IsNullOrEmpty(c.Email) && c.Email.Trim().Equals(trimmedEmail, StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException($"Another customer with email address '{trimmedEmail}' already exists.");
            }
        }

        customer.Name = dto.Name.Trim();
        customer.Email = dto.Email?.Trim() ?? string.Empty;
        customer.PhoneNumber = dto.PhoneNumber.Trim();
        customer.Address = dto.Address?.Trim() ?? string.Empty;
        customer.IsActive = dto.IsActive;
        customer.UpdatedAt = DateTime.UtcNow;

        await _customerRepository.UpdateAsync(customer);
        await _unitOfWork.SaveChangesAsync();

        var allSales = await _saleRepository.GetAllAsync();
        var cSales = allSales.Where(s => s.CustomerId == customer.Id && s.Status != "Cancelled").ToList();

        return new CustomerDto
        {
            Id = customer.Id,
            Name = customer.Name,
            Email = customer.Email,
            PhoneNumber = customer.PhoneNumber,
            Address = customer.Address,
            IsActive = customer.IsActive,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt,
            TotalSalesCount = cSales.Count,
            TotalSpent = cSales.Sum(s => s.TotalAmount)
        };
    }

    public async Task DeleteAsync(Guid id)
    {
        var customer = await _customerRepository.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Customer with ID {id} not found.");

        var allSales = await _saleRepository.GetAllAsync();
        if (allSales.Any(s => s.CustomerId == id && s.Status != "Cancelled"))
        {
            throw new InvalidOperationException($"Cannot delete customer '{customer.Name}' because they have associated sales records. Consider marking the customer as inactive instead.");
        }

        await _customerRepository.SoftDeleteAsync(customer.Id);
        await _unitOfWork.SaveChangesAsync();
    }
}
