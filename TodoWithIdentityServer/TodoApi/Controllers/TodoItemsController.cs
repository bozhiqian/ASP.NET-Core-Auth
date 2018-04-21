using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TodoApi.Helpers;
using TodoApi.Services;
using TodoViewModel;

namespace TodoApi.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    [Authorize]
    public class TodoItemsController : Controller
    {
        private readonly ITodoRepository _todoRepository;

        public TodoItemsController(ITodoRepository todoRepository)
        {
            _todoRepository = todoRepository;
        }

        [HttpGet()]
        public IActionResult GetTodoItems()
        {
            /*
             this.User can be used at the level of the web client, it can also be used at the level of the web api. 
             At client level the user object is constructed from the indentity token. 
             At API level, it is constructed from the access token. 
             So we access the current user's claims and look for the subclaim that identify the current user. 
            */
            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

            // get from repo
            var todoItems = _todoRepository.GetTodoItems(ownerId);

            // map to model
            var todooItemViewModels = Mapper.Map<IEnumerable<TodoItemViewModel>>(todoItems);

            // return
            return Ok(todooItemViewModels);
        }

        [HttpGet("{id}", Name = "GetTodoItem"), Authorize("MustOwnTodoItem")]
        public IActionResult GetTodoItems(long id)
        {
            var ownerId = User.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value; // this is how we know who the current user is.

            if (!_todoRepository.IsTodoItemOwner(id, ownerId))
            {
                return StatusCode(403);
            }

            var tooItem = _todoRepository.GetTodoItem(id);

            if (tooItem == null)
            {
                return NotFound();
            }

            var todoItemViewModel = Mapper.Map<TodoItemViewModel>(tooItem);

            return Ok(todoItemViewModel);
        }

        [HttpPost()]
        [Authorize(Roles = "FreeUser")]
        public IActionResult CreateTodoItems([FromBody] TodoItemForCreationViewModel todoItemForCreationViewModel)
        {
            if (todoItemForCreationViewModel == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                // return 422 - Unprocessable Entity when validation fails
                return new UnprocessableEntityObjectResult(ModelState);
            }

            var todoItem = Mapper.Map<Entities.TodoItem>(todoItemForCreationViewModel);

            var ownerId = User.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;
            todoItem.OwnerId = ownerId;

            // add and save.  
            _todoRepository.AddTodoItem(todoItem);

            if (!_todoRepository.Save())
            {
                throw new Exception($"Adding an todoitem failed on save.");
            }

            var todoItemViewModel = Mapper.Map<TodoItemViewModel>(todoItem);

            return CreatedAtRoute("GetTodoItem",
                new { id = todoItemViewModel.Id },
                todoItemViewModel);
        }

        [HttpDelete("{id}")]
        [Authorize("MustOwnTodoItem", Roles = "FreeUser")]
        public IActionResult DeleteTodoItems(long id)
        {
            var ownerId = User.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value; // this is how we know who the current user is.

            if (!_todoRepository.IsTodoItemOwner(id, ownerId))
            {
                return StatusCode(403);
            }

            var todoItem = _todoRepository.GetTodoItem(id);

            if (todoItem == null)
            {
                return NotFound();
            }

            _todoRepository.DeleteTodoItem(todoItem);

            if (!_todoRepository.Save())
            {
                throw new Exception($"Deleting todoitem with {id} failed on save.");
            }

            return NoContent();
        }

        [HttpPut("{id}")]
        [Authorize("MustOwnTodoItem", Roles = "FreeUser")]
        public IActionResult UpdateTodoItem(long id, [FromBody] TodoItemForUpdateViewModel todoItemForUpdateViewModel)
        {
            var ownerId = User.Claims.FirstOrDefault(claim => claim.Type == "sub")?.Value; // this is how we know who the current user is.
            if (!_todoRepository.IsTodoItemOwner(id, ownerId))
            {
                return StatusCode(403);
            }

            if (todoItemForUpdateViewModel == null)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                // return 422 - Unprocessable Entity when validation fails
                return new UnprocessableEntityObjectResult(ModelState);
            }

            var todoItem = _todoRepository.GetTodoItem(id);
            if (todoItem == null)
            {
                return NotFound();
            }

            Mapper.Map(todoItemForUpdateViewModel, todoItem);

            _todoRepository.UpdateTodoItem(todoItem);

            if (!_todoRepository.Save())
            {
                throw new Exception($"Updating todoitem with {id} failed on save.");
            }

            return NoContent();
        }
    }
}