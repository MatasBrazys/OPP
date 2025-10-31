//./GameShared/Types/DTOs/PlayerDto.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameShared.Types.DTOs
{
    public class PlayerDto
    {
        public int Id { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Health { get; set; }
        public string RoleType { get; set; }
        public string RoleColor { get; set; }
    }
}
