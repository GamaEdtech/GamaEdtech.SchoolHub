﻿using FluentValidation;

namespace GamaEdtech.Back.Gateway.Rest.Cities;

public class AddCityDtoValidator : AbstractValidator<AddCityDto>
{
	public AddCityDtoValidator()
	{
		RuleFor(x => x.Name)
			.NotNull().WithMessage("name is required")
			.NotEmpty().WithMessage("name is required")
			.MaximumLength(100).WithMessage("name is too long");
	}
}