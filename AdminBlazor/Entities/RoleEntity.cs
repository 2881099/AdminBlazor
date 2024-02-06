﻿using BootstrapBlazor.Components;
using FreeSql.DataAnnotations;

namespace BootstrapBlazor.Components
{
    /// <summary>
    /// 角色
    /// </summary>
    public class RoleEntity : Entity
    {
        /// <summary>
        /// 名称
        /// </summary>
        [Column(StringLength = 50)]
        public string Name { get; set; }
        /// <summary>
        /// 描述
        /// </summary>
        [Column(StringLength = 500)]
        public string Description { get; set; }
        /// <summary>
        /// 系统
        /// </summary>
        public bool IsAdministrator { get; set; }

        [Navigate(ManyToMany = typeof(RoleUserEntity))]
        public List<UserEntity> Users { get; set; }

        [Navigate(ManyToMany = typeof(RoleMenuEntity))]
        public List<MenuEntity> Menus { get; set; }
    }

    public class RoleUserEntity
    {
        public long RoleId { get; set; }
        public long UserId { get; set; }

        public RoleEntity Role { get; set; }
        public UserEntity User { get; set; }
    }

    partial class MenuEntity
    {
        [Navigate(ManyToMany = typeof(RoleMenuEntity))]
        public List<RoleEntity> Roles { get; set; }
        [Navigate(nameof(RoleMenuEntity.MenuId))]
        public List<RoleMenuEntity> RoleMenus { get; set; }
    }
    public class RoleMenuEntity
    {
        public long RoleId { get; set; }
        public long MenuId { get; set; }

        public RoleEntity Role { get; set; }
        public MenuEntity Menu { get; set; }
    }
}

partial class UserEntity
{
    [Navigate(ManyToMany = typeof(RoleUserEntity))]
    public List<RoleEntity> Roles { get; set; }
    [Navigate(nameof(RoleUserEntity.UserId))]
    public List<RoleUserEntity> RoleUsers { get; set; }
}
