﻿using CodingTrainer.CodingTrainerModels;
using CodingTrainer.CodingTrainerWeb.ActionFilters;
using CodingTrainer.CodingTrainerWeb.ApiControllers;
using CodingTrainer.CodingTrainerWeb.Dependencies;
using CodingTrainer.CodingTrainerWeb.Models;
using RazorEngine;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace CodingTrainer.CodingTrainerWeb.Controllers
{
    public class ExerciseController : Controller
    {
        ICodingTrainerRepository rep;
        IUserServices userServices;

        public ExerciseController(ICodingTrainerRepository repository, IUserServices userServices)
        {
            rep = repository;
            this.userServices = userServices;
        }

        [Authorize]
        public ActionResult Playground()
        {
            return View();
        }

        [AuthorizeExercise]
        public async Task<ActionResult> Exercise(int chapter, int exercise)
        {
            var exerciseDetails = await rep.GetExerciseAsync(chapter, exercise);
            if (exerciseDetails == null) throw new HttpException(404, "Not found");
            return View(exerciseDetails);
        }

        [ChildActionOnly]
        public ActionResult ExerciseSidebar(Exercise currentExercise)
        {
            async Task<ActionResult> ExerciseSidebarAsync()
            {
                ViewBag.CurrentExercise = currentExercise;

                ViewBag.User = await userServices.GetCurrentUserAsync();
                return PartialView(await rep.GetAllChaptersAsync());
            }

            return RunWithoutSyncContext(() => ExerciseSidebarAsync());
        }

        [Authorize]
        [ChildActionOnly]
        public ActionResult RunCode(Exercise exercise)
        {
            async Task<ActionResult> RunCodeAsync(Exercise _exercise)
            {
                string activeTheme = await userServices.GetCodeMirrorThemeAsync();
                return RunCode(_exercise, activeTheme);
            }

            return RunWithoutSyncContext(() => RunCodeAsync(exercise));
        }

        [Authorize]
        [ChildActionOnly]
        public ActionResult RunCodeById(int chapter, int exercise)
        {
            async Task<ActionResult> RunCodeAsync(int _chapter, int _exercise)
            {
                Task<string> themeTask = userServices.GetCodeMirrorThemeAsync();
                Task<Exercise> exerciseTask = rep.GetExerciseAsync(_chapter, _exercise);

                await Task.WhenAll(themeTask, exerciseTask);

                string activeTheme = themeTask.Result;
                Exercise model = exerciseTask.Result;

                return RunCode(model, activeTheme);
            }

            return RunWithoutSyncContext(() => RunCodeAsync(chapter, exercise));
        }

        private ActionResult RunCode(Exercise model, string activeTheme)
        {
            ViewBag.Theme = CodeMirrorThemes.Themes.ConvertAll(t => new SelectListItem()
                { Text = char.ToUpper(t[0]) + t.Substring(1), Value = t, Selected = t == activeTheme });

            return PartialView("RunCode", model);
        }

        [Authorize]
        [ChildActionOnly]
        public ActionResult Content(Exercise exercise)
        {
            // Make a razor engine service with the System.Web.Mvc namespace open
            var config = new TemplateServiceConfiguration();
            var namespaces = config.Namespaces;
            namespaces.Add("System.Web.Mvc");
            var service = RazorEngineService.Create(config);
            
            // Make the UrlHelper extension methods available
            string fullSource = "@{var Url = new UrlHelper(System.Web.HttpContext.Current.Request.RequestContext);} "
                    + exercise.Content;

            // Including the hash code in the key allows this to work even if the source changes
            // However, this will result in memory leaks. The fix for this is to write a new
            // caching provider - however, this is considered to be a minor issue, and is
            // acceptable for now - see this post by Matthias Dittrich
            // https://github.com/Antaris/RazorEngine/issues/232#issuecomment-128802285
            var key = $"ContentTemplate{exercise.ChapterNo}-{exercise.ExerciseNo}-{fullSource.GetHashCode()}";
            service.AddTemplate(key, new LoadedTemplateSource(fullSource));
            
            // Finally, compile and run the content source
            return Content(service.RunCompile(key));
        }

        // Helpers

        private ActionResult RunWithoutSyncContext(Func<Task<ActionResult>> task)
        {
            var syncContext = SynchronizationContext.Current;
            SynchronizationContext.SetSynchronizationContext(null); // Can't run async from partial views without this

            var result = task().Result;

            SynchronizationContext.SetSynchronizationContext(syncContext);
            return result;
        }
    }
}