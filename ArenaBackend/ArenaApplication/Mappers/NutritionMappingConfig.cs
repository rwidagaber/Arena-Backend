using ArenaApplication.Dtos.Nutrition;
using ArenaDomain.Entities.Nutrition;
using Mapster;
using System;
using System.Collections.Generic;
using System.Text;

namespace ArenaApplication.Mappers
{
    public class NutritionMappingConfig : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // NutritionPlan → NutritionPlanResponseDto
            config.NewConfig<NutritionPlan, NutritionPlanResponseDto>()
                .Map(dest => dest.Meals, src => src.Meals);

            // Meal → MealResponseDto
            config.NewConfig<Meal, MealResponseDto>()
                .Map(dest => dest.ProteinGrams, src => src.Protein)
                .Map(dest => dest.CarbsGrams, src => src.Carbs)
                .Map(dest => dest.FatGrams, src => src.Fat); 

            // MealLog → MealLogResponseDto
            config.NewConfig<MealLog, MealLogResponseDto>();

            
        }
    }
}
