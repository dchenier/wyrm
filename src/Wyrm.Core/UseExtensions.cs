using System;
using System.Threading.Tasks;

namespace Wyrm.Events.Builder
{
    public static class UseExtensions
    {
        public static IEventBuilder Use(this IEventBuilder builder, 
            Func<EventContext, Func<Task>, Task> middleware)
        {
            return builder.Use(next => 
            {
               return context =>
               {
                   Func<Task> simpleNext = () => next(context);
                   return middleware(context, simpleNext);
               } ;
            });
        }
    }
}