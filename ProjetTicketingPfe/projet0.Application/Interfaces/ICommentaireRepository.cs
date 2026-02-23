using System;
using System.Collections.Generic;
using System.Text;
using projet0.Domain.Entities;

namespace projet0.Application.Interfaces
{
    public interface ICommentaireRepository : IGenericRepository<CommentaireTicket>
    {
        Task<List<CommentaireTicket>> GetCommentairesByTicketIdAsync(Guid ticketId);
        Task<CommentaireTicket> GetCommentaireWithPiecesJointesAsync(Guid id);
        Task<CommentaireTicket> GetCommentaireForUpdateAsync(Guid id);
        Task<bool> HasPiecesJointesAsync(Guid commentaireId);
        Task DeleteCommentaireWithPiecesJointesAsync(Guid id);
    }
}
