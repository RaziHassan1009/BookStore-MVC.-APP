using GrapheneSensore.Data;
using GrapheneSensore.Models;
using GrapheneSensore.Logging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace GrapheneSensore.Services
{
    /// <summary>
    /// Service for managing templates, sections, and codes (User Story 4, 13, 14)
    /// </summary>
    public class TemplateService
    {
        #region Template Operations

        /// <summary>
        /// Gets all active templates (User Story 4)
        /// </summary>
        public async Task<List<Template>> GetAllTemplatesAsync(bool includeInactive = false)
        {
            try
            {
                using var context = new SensoreDbContext();
                var query = context.Templates.AsQueryable();
                
                if (!includeInactive)
                {
                    query = query.Where(t => t.IsActive);
                }

                return await query.OrderBy(t => t.DisplayOrder).ThenBy(t => t.TemplateName).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error retrieving templates", ex, "TemplateService");
                throw;
            }
        }

        /// <summary>
        /// Gets a template with its sections (User Story 4, 7)
        /// </summary>
        public async Task<(Template? template, List<Section> sections)> GetTemplateWithSectionsAsync(Guid templateId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var template = await context.Templates.FindAsync(templateId);
                
                if (template == null)
                {
                    return (null, new List<Section>());
                }

                var sections = await context.TemplateSectionLinks
                    .Where(tsl => tsl.TemplateId == templateId)
                    .Include(tsl => tsl.Section)
                    .OrderBy(tsl => tsl.DisplayOrder)
                    .Select(tsl => tsl.Section!)
                    .ToListAsync();

                return (template, sections);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving template with sections: {templateId}", ex, "TemplateService");
                throw;
            }
        }

        /// <summary>
        /// Adds a new template (User Story 13)
        /// </summary>
        public async Task<(bool success, string message, Template? template)> AddTemplateAsync(Template template)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(template.TemplateName))
                {
                    return (false, "Template name is required", null);
                }

                using var context = new SensoreDbContext();
                context.Templates.Add(template);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Template added: {template.TemplateName}", "TemplateService");
                return (true, "Template added successfully", template);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding template: {template.TemplateName}", ex, "TemplateService");
                return (false, "An error occurred while adding the template", null);
            }
        }

        /// <summary>
        /// Updates a template (User Story 13)
        /// </summary>
        public async Task<(bool success, string message)> UpdateTemplateAsync(Template template)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.Templates.Update(template);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Template updated: {template.TemplateName}", "TemplateService");
                return (true, "Template updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating template: {template.TemplateId}", ex, "TemplateService");
                return (false, "An error occurred while updating the template");
            }
        }

        /// <summary>
        /// Deletes a template (User Story 13)
        /// </summary>
        public async Task<(bool success, string message)> DeleteTemplateAsync(Guid templateId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var template = await context.Templates.FindAsync(templateId);
                
                if (template == null)
                {
                    return (false, "Template not found");
                }

                context.Templates.Remove(template);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Template deleted: {templateId}", "TemplateService");
                return (true, "Template deleted successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting template: {templateId}", ex, "TemplateService");
                return (false, "An error occurred while deleting the template");
            }
        }

        #endregion

        #region Section Operations

        /// <summary>
        /// Gets all active sections (User Story 13, 14)
        /// </summary>
        public async Task<List<Section>> GetAllSectionsAsync(bool includeInactive = false)
        {
            try
            {
                using var context = new SensoreDbContext();
                var query = context.Sections.AsQueryable();
                
                if (!includeInactive)
                {
                    query = query.Where(s => s.IsActive);
                }

                return await query.OrderBy(s => s.DisplayOrder).ThenBy(s => s.SectionName).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError("Error retrieving sections", ex, "TemplateService");
                throw;
            }
        }

        /// <summary>
        /// Adds a new section (User Story 13)
        /// </summary>
        public async Task<(bool success, string message, Section? section)> AddSectionAsync(Section section)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(section.SectionName))
                {
                    return (false, "Section name is required", null);
                }

                using var context = new SensoreDbContext();
                context.Sections.Add(section);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Section added: {section.SectionName}", "TemplateService");
                return (true, "Section added successfully", section);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding section: {section.SectionName}", ex, "TemplateService");
                return (false, "An error occurred while adding the section", null);
            }
        }

        /// <summary>
        /// Updates a section (User Story 13)
        /// </summary>
        public async Task<(bool success, string message)> UpdateSectionAsync(Section section)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.Sections.Update(section);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Section updated: {section.SectionName}", "TemplateService");
                return (true, "Section updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating section: {section.SectionId}", ex, "TemplateService");
                return (false, "An error occurred while updating the section");
            }
        }

        /// <summary>
        /// Deletes a section (User Story 13)
        /// </summary>
        public async Task<(bool success, string message)> DeleteSectionAsync(Guid sectionId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var section = await context.Sections.FindAsync(sectionId);
                
                if (section == null)
                {
                    return (false, "Section not found");
                }

                context.Sections.Remove(section);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Section deleted: {sectionId}", "TemplateService");
                return (true, "Section deleted successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting section: {sectionId}", ex, "TemplateService");
                return (false, "An error occurred while deleting the section");
            }
        }

        #endregion

        #region Code Operations

        /// <summary>
        /// Gets all codes for a section (User Story 8)
        /// </summary>
        public async Task<List<Code>> GetCodesBySectionAsync(Guid sectionId, bool includeInactive = false)
        {
            try
            {
                using var context = new SensoreDbContext();
                var query = context.Codes.Where(c => c.SectionId == sectionId);
                
                if (!includeInactive)
                {
                    query = query.Where(c => c.IsActive);
                }

                return await query.OrderBy(c => c.DisplayOrder).ThenBy(c => c.CodeText).ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving codes for section: {sectionId}", ex, "TemplateService");
                throw;
            }
        }

        /// <summary>
        /// Adds a new code (User Story 13)
        /// </summary>
        public async Task<(bool success, string message, Code? code)> AddCodeAsync(Code code)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(code.CodeText))
                {
                    return (false, "Code text is required", null);
                }

                using var context = new SensoreDbContext();
                context.Codes.Add(code);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Code added: {code.CodeText}", "TemplateService");
                return (true, "Code added successfully", code);
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error adding code: {code.CodeText}", ex, "TemplateService");
                return (false, "An error occurred while adding the code", null);
            }
        }

        /// <summary>
        /// Updates a code (User Story 13)
        /// </summary>
        public async Task<(bool success, string message)> UpdateCodeAsync(Code code)
        {
            try
            {
                using var context = new SensoreDbContext();
                context.Codes.Update(code);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Code updated: {code.CodeText}", "TemplateService");
                return (true, "Code updated successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error updating code: {code.CodeId}", ex, "TemplateService");
                return (false, "An error occurred while updating the code");
            }
        }

        /// <summary>
        /// Deletes a code (User Story 13)
        /// </summary>
        public async Task<(bool success, string message)> DeleteCodeAsync(Guid codeId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var code = await context.Codes.FindAsync(codeId);
                
                if (code == null)
                {
                    return (false, "Code not found");
                }

                context.Codes.Remove(code);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Code deleted: {codeId}", "TemplateService");
                return (true, "Code deleted successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error deleting code: {codeId}", ex, "TemplateService");
                return (false, "An error occurred while deleting the code");
            }
        }

        #endregion

        #region Template-Section Link Operations

        /// <summary>
        /// Links a section to a template (User Story 14)
        /// </summary>
        public async Task<(bool success, string message)> LinkSectionToTemplateAsync(Guid templateId, Guid sectionId, int displayOrder, bool isRequired = true)
        {
            try
            {
                using var context = new SensoreDbContext();
                
                // Check if link already exists
                var existingLink = await context.TemplateSectionLinks
                    .FirstOrDefaultAsync(tsl => tsl.TemplateId == templateId && tsl.SectionId == sectionId);
                
                if (existingLink != null)
                {
                    return (false, "This section is already linked to the template");
                }

                var link = new TemplateSectionLink
                {
                    TemplateId = templateId,
                    SectionId = sectionId,
                    DisplayOrder = displayOrder,
                    IsRequired = isRequired
                };

                context.TemplateSectionLinks.Add(link);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Section {sectionId} linked to template {templateId}", "TemplateService");
                return (true, "Section linked to template successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error linking section to template: {templateId}, {sectionId}", ex, "TemplateService");
                return (false, "An error occurred while linking the section to the template");
            }
        }

        /// <summary>
        /// Unlinks a section from a template (User Story 14)
        /// </summary>
        public async Task<(bool success, string message)> UnlinkSectionFromTemplateAsync(Guid templateId, Guid sectionId)
        {
            try
            {
                using var context = new SensoreDbContext();
                var link = await context.TemplateSectionLinks
                    .FirstOrDefaultAsync(tsl => tsl.TemplateId == templateId && tsl.SectionId == sectionId);
                
                if (link == null)
                {
                    return (false, "Link not found");
                }

                context.TemplateSectionLinks.Remove(link);
                await context.SaveChangesAsync();

                Logger.Instance.LogInfo($"Section {sectionId} unlinked from template {templateId}", "TemplateService");
                return (true, "Section unlinked from template successfully");
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error unlinking section from template: {templateId}, {sectionId}", ex, "TemplateService");
                return (false, "An error occurred while unlinking the section from the template");
            }
        }

        /// <summary>
        /// Gets all linked sections for a template (User Story 14)
        /// </summary>
        public async Task<List<TemplateSectionLink>> GetTemplateSectionLinksAsync(Guid templateId)
        {
            try
            {
                using var context = new SensoreDbContext();
                return await context.TemplateSectionLinks
                    .Where(tsl => tsl.TemplateId == templateId)
                    .Include(tsl => tsl.Section)
                    .OrderBy(tsl => tsl.DisplayOrder)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                Logger.Instance.LogError($"Error retrieving template section links: {templateId}", ex, "TemplateService");
                throw;
            }
        }

        #endregion
    }
}
