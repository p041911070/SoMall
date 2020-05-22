﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using TT.Abp.AppManagement.Apps;
using TT.Abp.Mall.Application.Products.Dtos;
using TT.Abp.Mall.Application.Shops;
using TT.Abp.Mall.Definitions;
using TT.Abp.Mall.Domain.Products;
using TT.Abp.Mall.Domain.Shares;
using TT.Abp.Mall.Domain.Shops;
using TT.Abp.Mall.Handlers;
using TT.Extensions;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Guids;

namespace TT.Abp.Mall.Application.Products
{
    public class ProductSpuAppService
        : CrudAppService<ProductSpu, ProductSpuDto, Guid, MallRequestDto, SpuCreateOrUpdateDto, SpuCreateOrUpdateDto>, IProductSpuAppService
    {
        private readonly IGuidGenerator _guidGenerator;
        private readonly IRepository<ProductSku, Guid> _skuRepository;
        private readonly IRepository<ProductCategory, Guid> _categoryRepository;
        private readonly IRepository<AppProductSpu> _appProductRepository;
        private readonly IRepository<QrDetail, Guid> _qrDetailRepository;
        private readonly IMallShopRepository _mallShopRepository;
        private readonly IMallShopLookupService _mallShopLookupService;
        private readonly IAppDefinitionManager _appDefinitionManager;
        private readonly IMediator _mediator;

        public ProductSpuAppService(
            IGuidGenerator guidGenerator,
            IRepository<ProductSpu, Guid> repository,
            IRepository<ProductSku, Guid> skuRepository,
            IRepository<ProductCategory, Guid> categoryRepository,
            IRepository<AppProductSpu> appProductRepository,
            IRepository<QrDetail, Guid> qrDetailRepository,
            IMallShopRepository mallShopRepository,
            IMallShopLookupService mallShopLookupService,
            IAppDefinitionManager appDefinitionManager,
            IMediator mediator) : base(repository)
        {
            base.GetListPolicyName = MallPermissions.Products.Default;
            base.CreatePolicyName = MallPermissions.Products.Create;
            base.UpdatePolicyName = MallPermissions.Products.Update;
            base.DeletePolicyName = MallPermissions.Products.Delete;

            _guidGenerator = guidGenerator;
            _skuRepository = skuRepository;
            _categoryRepository = categoryRepository;
            _appProductRepository = appProductRepository;
            _qrDetailRepository = qrDetailRepository;
            _mallShopRepository = mallShopRepository;
            _mallShopLookupService = mallShopLookupService;
            _appDefinitionManager = appDefinitionManager;
            _mediator = mediator;
        }

        public override async Task<ProductSpuDto> GetAsync(Guid id)
        {
            await CheckGetPolicyAsync();
            var entity = await Repository.Include(x => x.Skus).FirstOrDefaultAsync(x => x.Id == id);
            return MapToGetOutputDto(entity);
        }


        public override async Task<ProductSpuDto> CreateAsync(SpuCreateOrUpdateDto input)
        {
            var local = await Repository.FirstOrDefaultAsync(x => x.Code == input.Code || (x.Name == input.Name && x.CategoryId == input.CategoryId));

            if (local != null)
            {
                throw new UserFriendlyException("同分类下不能同名或商品编号相同");
            }

            var entity = MapToEntity(input);

            TryToSetTenantId(entity);

            await Repository.InsertAsync(entity);

            foreach (var skuInput in input.Skus)
            {
                skuInput.SpuId = entity.Id;
                var sku = ObjectMapper.Map<SkuCreateOrUpdateDto, ProductSku>(skuInput);
                sku.NewId(_guidGenerator);
                sku.SetTenant(entity.TenantId);
                await _skuRepository.InsertAsync(sku);
            }

            return MapToGetOutputDto(entity);
        }


        public override async Task<ProductSpuDto> UpdateAsync(Guid id, SpuCreateOrUpdateDto input)
        {
            if (await Repository
                .AnyAsync(x => (x.Code == input.Code || (x.Name == input.Name && x.CategoryId == input.CategoryId)) && x.Id != id))
            {
                throw new UserFriendlyException("同分类下不能同名或商品编号相同");
            }


            await CheckUpdatePolicyAsync();

            var entity = await Repository
                .Include(x => x.AppProductSpus)
                .Include(x => x.Skus).FirstOrDefaultAsync(x => x.Id == id);

            input.DescCommon = Regex.Replace(input.DescCommon, @"(style=""height:\d+px; width:\d+px"")", @"class=""img""");

            ObjectMapper.Map(input, entity);


            await Repository.UpdateAsync(entity, autoSave: true);

            var dbIds = entity.Skus.Select(x => x.Id).ToList();

            foreach (var skuInput in input.Skus)
            {
                var sku = entity.Skus.FirstOrDefault(x => x.Id == skuInput.Id);
                if (sku != null)
                {
                    dbIds.Remove(sku.Id);
                    skuInput.Id = sku.Id;
                    skuInput.Desc = Regex.Replace(skuInput.Desc, @"(style=""height:\d+px; width:\d+px"")", @"class=""img""");
                    ObjectMapper.Map(skuInput, sku);
                    await _skuRepository.UpdateAsync(sku, autoSave: true);
                }
                else
                {
                    skuInput.SpuId = entity.Id;
                    sku = ObjectMapper.Map<SkuCreateOrUpdateDto, ProductSku>(skuInput);
                    sku.NewId(_guidGenerator);
                    sku.Desc = Regex.Replace(sku.Desc, @"(style=""height:\d+px; width:\d+px"")", @"class=""img""");
                    sku.SetTenant(entity.TenantId);
                    await _skuRepository.InsertAsync(sku);
                }
            }

            //删除前端已删除的
            foreach (var noUsed in dbIds)
            {
                await _skuRepository.DeleteAsync(noUsed);
            }


            #region apps

            foreach (var jo in input.Apps)
            {
                var appName = jo["value"] + "";
                var value = Convert.ToBoolean(jo["checked"]);
                if (value)
                {
                    if ((entity.AppProductSpus).All(x => x.AppName != appName))
                    {
                        await _appProductRepository.InsertAsync(new AppProductSpu(
                            appName, id, entity.TenantId), autoSave: true);
                    }
                }
                else
                {
                    if (entity.AppProductSpus != null && entity.AppProductSpus.Count > 0)
                    {
                        var existCate = entity.AppProductSpus.FirstOrDefault(x => x.AppName == appName);
                        if (existCate != null)
                            await _appProductRepository.DeleteAsync(existCate, autoSave: true);
                    }
                }
            }

            #endregion


            return MapToGetOutputDto(entity);
        }

        /// <summary>
        /// 获取编辑
        /// </summary>
        /// <param name="id"></param>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<GetForEditOutput<SpuCreateOrUpdateDto>> GetForEdit(Guid id)
        {
            var find = await Repository
                .Include(x => x.Skus)
                .Include(x => x.AppProductSpus)
                .FirstOrDefaultAsync(z => z.Id == id);

            var schema = JToken.FromObject(new { });

            var categoryList = await _categoryRepository.GetListAsync();
            schema["categoryId"] = categoryList.GetSelection("string", "categoryId", @"{0}", new[] {"Name"}, "Id");

            var shops = await _mallShopRepository.GetListAsync();
            schema["shopId"] = shops.GetSelection("string", "shopId", @"{0}", new[] {"Name"}, "Id");

            var apps = _appDefinitionManager.GetAll();
            schema["apps"] = apps.GetSelection("string", "appName", @"{0}", new[] {"Name"}, "Name");

            return new GetForEditOutput<SpuCreateOrUpdateDto>(
                ObjectMapper.Map<ProductSpu, SpuCreateOrUpdateDto>(find ?? new ProductSpu()
                {
                    Skus = new List<ProductSku>()
                    {
                        new ProductSku("name")
                    }
                }), schema);
        }

        public override async Task<PagedResultDto<ProductSpuDto>> GetListAsync(MallRequestDto input)
        {
            var spuDtos = await base.GetListAsync(input);

            var shopDictionary = new Dictionary<Guid, MallShopDto>();

            foreach (var spuDto in spuDtos.Items)
            {
                if (spuDto.ShopId.HasValue)
                {
                    if (!shopDictionary.ContainsKey(spuDto.ShopId.Value))
                    {
                        var shop = await _mallShopLookupService.FindByIdAsync(spuDto.ShopId.Value);
                        if (shop != null)
                        {
                            shopDictionary[shop.Id] = ObjectMapper.Map<MallShop, MallShopDto>(shop);
                        }
                    }

                    if (shopDictionary.ContainsKey(spuDto.ShopId.Value))
                    {
                        spuDto.Shop = shopDictionary[(Guid) spuDto.ShopId];
                    }
                }
            }

            return spuDtos;
        }

        protected override IQueryable<ProductSpu> CreateFilteredQuery(MallRequestDto input)
        {
            return Repository
                .Include(x => x.Category)
                .Include(x => x.AppProductSpus)
                .Include(x => x.Skus)
                .WhereIf(input.ShopId.HasValue, x => x.ShopId == input.ShopId)
                .WhereIf(input.ShopId.HasValue, x => x.ShopId == input.ShopId);
        }


        [HttpPost]
        public async Task<QrDetail> GetQr(MallRequestDto input)
        {
            var result = await _mediator.Send(new GetQrQuery(input, "mall_product_page"));
            return result;
        }
    }
}