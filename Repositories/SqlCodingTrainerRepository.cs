﻿using CodingTrainer.CodingTrainerEntityFramework.Contexts;
using CodingTrainer.CodingTrainerModels;
using CodingTrainer.CodingTrainerWeb.Dependencies;
using CodingTrainer.CSharpRunner.Assessment;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodingTrainer.Repositories
{
    public class SqlCodingTrainerRepository : ICodingTrainerRepository
    {
        ApplicationDbContext context = new ApplicationDbContext();

        // Chapters

        public async Task<IEnumerable<Chapter>> GetAllChaptersAsync()
        {
            // Ignore chapters <0 - these will be things like the Playground which
            // are not real chapters
            var chapters = await context.Chapters.Where(c => c.ChapterNo >= 0).Include("Exercises").OrderBy(c => c.ChapterNo).ToListAsync();
            foreach (var chapter in chapters)
            {
                chapter.Exercises.Sort();
            }
            return chapters;
        }

        // Exercises

        public async Task<Exercise> GetExerciseAsync(int chapterNo, int exercisesNo)
        {
            var exercise = from e in context.Exercises
                           where e.ChapterNo == chapterNo && e.ExerciseNo == exercisesNo
                           select e;
            return await exercise.SingleOrDefaultAsync();
        }
        public Exercise GetExercise(int chapterNo, int exercisesNo)
        {
            var exercise = from e in context.Exercises
                           where e.ChapterNo == chapterNo && e.ExerciseNo == exercisesNo
                           select e;
            return exercise.SingleOrDefault();
        }

        // Assessments

        public async Task<IEnumerable<AssessmentGroup>> GetAssessmentGroupsForExerciseAsync(int chapterNo, int exerciseNo)
        {
            return await context.AssessmentGroups.Where(g => g.ChapterNo == chapterNo && g.ExerciseNo == exerciseNo)
                .OrderBy(g => g.Sequence)
                .Include(g => g.Assessments)
                .ToListAsync();
        }

        public async Task InsertSubmissionAsync(Submission submission)
        {
            context.Submissions.Add(submission);
            await context.SaveChangesAsync();
        }

        public async Task<IEnumerable<Submission>> GetSubmissionsAsync(int chapterNo, int exerciseNo, string userId)
        {
            return await context.Submissions.Where(
                c => c.ChapterNo == chapterNo
                  && c.ExerciseNo == exerciseNo
                  && c.UserId == userId).OrderByDescending(c => c.SubmissionDateTime).ToListAsync();
        }

        public async Task<Submission> GetSubmissionAsync(int submissionId)
        {
            return await context.Submissions.SingleOrDefaultAsync(s => s.SubmissionId == submissionId);
        }

        public Submission GetSubmission(int submissionId)
        {
            return context.Submissions.SingleOrDefault(s => s.SubmissionId == submissionId);
        }

        // Exception logs

        public async Task InsertExceptionLogAsync(ExceptionRunningUsersCode log)
        {
            context.ExceptionLogs.Add(log);
            await context.SaveChangesAsync();
        }

        // Unhandled exceptions

        public void InsertUnhandledControllerException(UnhandledControllerException exception)
        {
            context.UnhandledControllerExceptions.Add(exception);
            context.SaveChanges();
        }

        // Saved work

        public async Task SaveWorkAsync(SavedWork savedWork)
        {
            var existingData = context.SavedWork.Where(s => s.UserId == savedWork.UserId 
                                                         && s.ChapterNo == savedWork.ChapterNo
                                                         && s.ExerciseNo == savedWork.ExerciseNo);
            if (await existingData.AnyAsync())
            {
                savedWork.SavedWorkId = existingData.Single().SavedWorkId;
                context.Entry(existingData.Single()).CurrentValues.SetValues(savedWork);
                await context.SaveChangesAsync();
            }
            else
            {
                context.SavedWork.Add(savedWork);
                await context.SaveChangesAsync();
            }
        }

        public async Task<SavedWork> GetSavedWorkAsync(int chapter, int exercise, string userId)
        {
            return await context.SavedWork.SingleOrDefaultAsync(s => s.ChapterNo == chapter
                                                                  && s.ExerciseNo == exercise
                                                                  && s.UserId == userId);
        }
    }

}
