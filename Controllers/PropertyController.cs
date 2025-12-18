using Microsoft.AspNetCore.Mvc;
using property_service.Interfaces;
using property_service.Models.PropertyModels;

namespace property_service.Controllers;

[ApiController]
[Route("property")]
[Produces("application/json")]
public class PropertyController : ControllerBase
{
    private readonly IPropertyService _propertyService;

    public PropertyController(IPropertyService propertyService)
    {
        _propertyService = propertyService;
    }

    // GET /property
    [HttpGet]
    [EndpointSummary("Get all properties")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(List<Property>))]
    public async Task<ActionResult<List<Property>>> GetProperties([FromQuery] PropertyFilter filter)
    {
        var properties = await _propertyService.GetPropertiesAsync(filter);
        return Ok(properties);
    }

    // GET /property/{id}
    [HttpGet("{id:int}")]
    [EndpointSummary("Retrieves the property matching the specified ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(Property))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<Property>> GetById(int id)
    {
        var property = await _propertyService.GetPropertyByIdAsync(id);
        if (property == null)
            return NotFound();

        return Ok(property);
    }

    // POST /property
    [HttpPost]
    [EndpointSummary("Creates a new property with optional images.")]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<int>> Insert(
        [FromForm] PropertyCreateRequestDTO dto)
    {
        var propertyId = await _propertyService.InsertPropertyAsync(dto);
        return Ok(propertyId);
    }

    // PUT /property/{id}
    [HttpPut("{id:int}")]
    [EndpointSummary("Updates the property matching the specified ID.")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(int))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Consumes("application/json")]
    public async Task<ActionResult<int>> Update(
        int id,
        [FromBody] PropertyCreateRequestDTO dto)
    {
        var updatedId = await _propertyService.UpdatePropertyAsync(id, dto);
        if (updatedId == null)
            return NotFound();

        return Ok(updatedId);
    }

    // DELETE /property/{id}
    [HttpDelete("{id:int}")]
    [EndpointSummary("Deletes the property matching the specified ID and all its images.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> Delete(int id)
    {
        var deletedId = await _propertyService.DeletePropertyByIdAsync(id);
        if (deletedId == null)
            return NotFound();

        return Ok();
    }
}
