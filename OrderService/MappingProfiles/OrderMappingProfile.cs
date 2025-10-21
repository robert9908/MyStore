using AutoMapper;
using OrderService.Entities;
using OrderService.DTOs;

namespace OrderService.MappingProfiles;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderResponse>()
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

        CreateMap<OrderItem, OrderItemDto>()
            .ReverseMap();

        CreateMap<CreateOrderRequest, Order>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => OrderStatus.Pending))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));
    }
}
