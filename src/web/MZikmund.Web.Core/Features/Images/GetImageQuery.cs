using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace MZikmund.Web.Core.Features.Images;

public record GetImageQuery : IRequest<byte[]>;
