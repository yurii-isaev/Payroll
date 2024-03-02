using AutoMapper;
using Payroll.Domains.Entities;
using Payroll.Services.Contracts;
using Payroll.Services.Contracts.Services;
using Payroll.Services.Exceptions;
using Payroll.Services.Objects;

namespace Payroll.Services;

public class EmployeeService : IEmployeeService
{
    readonly IEmployeeRepository _repository;
    readonly IMapper _mapper;
    readonly IWebHostEnvironment _environment;

    const string UploadDir = @"images/employees/";
    const string DefaultPhotoPath = @"/images/employees/unknown.jpg";

    public EmployeeService(IEmployeeRepository repository, IMapper mapper, IWebHostEnvironment environment)
    {
        _repository = repository;
        _mapper = mapper;
        _environment = environment;
    }

    public async Task CreateEmployeeAsync(EmployeeDto employeeDto)
    {
        var mapper = new MapperConfiguration(conf =>
        {
            conf.CreateMap<Employee, EmployeeDto>()
                .ForMember(dto => dto.FormFile, opt => opt.MapFrom(emp => emp.ImageName));
        });

        var employee = mapper.CreateMapper().Map<EmployeeDto, Employee>(employeeDto);

        employee.PaymentMethod = employeeDto.PaymentMethod.ToString();

        if (employeeDto.FormFile != null && employeeDto.FormFile.Length > 0)
        {
            await AddEmployeePhoto(employeeDto, employee);
        }
        else
        {
            await AddEmployeeDefaultPhoto(employeeDto);
        }

        if (employee.Name != null)
        {
            var nameExists = await _repository.EmployeeNameExistsAsync(employee.Name);

            if (!nameExists)
            {
                await _repository.CreateEmployeeAsync(employee);
            }
            else
            {
                throw new EmployeeExistsException("Employee already exists. The employee's name must be unique");
            }
        }
    }

    private Task AddEmployeeDefaultPhoto(EmployeeDto employeeDto)
    {
        var filename = "unknown.jpg";
        var filePath = Path.Combine(_environment.WebRootPath, UploadDir, filename);
        var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
        var fileInfo = new FileInfo(filePath);

        employeeDto.FormFile = new FormFile(
            fileStream,
            0,
            fileInfo.Length,
            "FormFile",
            Path.GetFileName(fileInfo.Name)
        );

        employeeDto.ImageName = $"/{UploadDir}{filename}";
        return Task.CompletedTask;
    }

    private async Task AddEmployeePhoto(EmployeeDto employeeDto, Employee employee)
    {
        if (employeeDto.FormFile != null)
        {
            // Get the image file extension
            var fileExtension = Path.GetExtension(employeeDto.FormFile.FileName);

            // Form the file name using the name from the DTO and the file extension 
            var filename = $"{employeeDto.Name}{fileExtension}";

            // Concatenate the path to the upload directory with the filename
            var path = Path.Combine(_environment.WebRootPath, UploadDir, filename);

            // Check if the file already exists and delete it if necessary
            if (File.Exists(path)) File.Delete(path);

            // Open the file for writing and create a new file
            await using var stream = new FileStream(
                path,
                FileMode.Create,
                FileAccess.Write, FileShare.None,
                bufferSize: 4096,
                useAsync: true
            );

            // Perform an asynchronous copy of the contents of the file from the DTO to the new file created by FileStream
            await employeeDto.FormFile.CopyToAsync(stream);

            // Assign the filename with a path to the corresponding property of the employee
            employee.ImageName = $"/{UploadDir}{filename}";
        }
        else if (employee.ImageName != DefaultPhotoPath)
        {
            // If no new image is selected, keep the existing image unchanged
            // You may need to adjust the path to match the location of your default image
            employee.ImageName = DefaultPhotoPath;
        }
    }

    public async Task<IEnumerable<EmployeeDto>> GetEmployeeListAsync(string keyword)
    {
        try
        {
            var employeeList = await _repository.GetEmployeeListAsync();

            if (!string.IsNullOrEmpty(keyword))
            {
                employeeList = employeeList.Where(emp => emp.Name!.Contains(keyword));
            }

            return _mapper.Map<IEnumerable<EmployeeDto>>(employeeList);
        }
        catch
        {
            return Enumerable.Empty<EmployeeDto>();
        }
    }

    public async Task<EmployeeDto> GetEmployeeByIdAsync(Guid employeeId)
    {
        var employee = await _repository.GetEmployeeByIdAsync(employeeId);

        var mapper = new MapperConfiguration(conf =>
        {
            conf.CreateMap<Employee, EmployeeDto>().ForMember(dto => dto.FormFile, opt => opt.Ignore());
            conf.CreateMap<EmployeeDto, Employee>();
        });

        var employeeDto = mapper.CreateMapper().Map<Employee, EmployeeDto>(employee);

        return employeeDto;
    }

    public async Task UpdateEmployeeAsync(EmployeeDto employeeDto)
    {
        var mapper = new MapperConfiguration(conf =>
        {
            conf.CreateMap<Employee, EmployeeDto>()
                .ForMember(dto => dto.FormFile, opt => opt.MapFrom(emp => emp.ImageName));

            conf.CreateMap<EmployeeDto, Employee>()
                .ForMember(emp => emp.ImageName, opt => opt.MapFrom(dto => dto.FormFile!.FileName));
        });

        var employee = mapper.CreateMapper().Map<EmployeeDto, Employee>(employeeDto);

        employee.PaymentMethod = employeeDto.PaymentMethod.ToString();

        if (employeeDto.FormFile != null && employeeDto.FormFile.Length > 0)
        {
            var fileExtension = Path.GetExtension(employeeDto.FormFile!.FileName);
            var filename = $"{employeeDto.Name}{fileExtension}";
            var newEmployeePhotoPath = Path.Combine(_environment.WebRootPath, UploadDir, filename);
            var employeeObj = await _repository.GetEmployeeByIdAsync(employeeDto.Id);
            var employeePhoto = employeeObj.ImageName;

            // Delete the old photo, if it exists and it's not the default static image
            if (!string.IsNullOrEmpty(employeePhoto) && employeePhoto != DefaultPhotoPath)
            {
                var oldEmployeePhoto = Path.Combine(_environment.WebRootPath, employeePhoto.TrimStart('/'));

                if (File.Exists(oldEmployeePhoto))
                {
                    await Task.Run(() => File.Delete(oldEmployeePhoto));
                }
            }

            // This ensures that the file is properly closed after the image is copied, even if an exception occurs
            using (var fileStream = new FileStream(newEmployeePhotoPath, FileMode.Create))
            {
                await employeeDto.FormFile!.CopyToAsync(fileStream);
            }

            employee.ImageName = $"/{UploadDir}{filename}";
        }

        await _repository.UpdateEmployeeAsync(employee);
    }


    public async Task DeleteEmployeeByIdAsync(Guid employeeId)
    {
        var dto = await GetEmployeeByIdAsync(employeeId);

        if (dto.ImageName != null && dto.ImageName.Length > 0)
        {
            var photoNamePath = dto.ImageName;
            await DeletePhotoFromStorage(photoNamePath);
        }

        await _repository.DeleteEmployeeAsync(employeeId);
    }

    private async Task DeletePhotoFromStorage(string photoNamePath)
    {
        var fullPath = $"wwwroot{photoNamePath}";

        if (File.Exists(fullPath))
        {
            // File.Delete doesn't have an asynchronous version
            await Task.Run(() => File.Delete(fullPath));
        }
    }
}
