using System;
using System.Collections.Generic;
using System.Reflection;
using Server.Commands;
using Server.Items;
using Server.Targeting;

namespace Server.Gumps
{
  public class SetObjectTarget : Target
  {
    private List<object> m_List;
    private Mobile m_Mobile;
    private object m_Object;
    private int m_Page;
    private PropertyInfo m_Property;
    private Stack<StackEntry> m_Stack;
    private Type m_Type;

    public SetObjectTarget(PropertyInfo prop, Mobile mobile, object o, Stack<StackEntry> stack, Type type, int page,
      List<object> list) : base(-1, false, TargetFlags.None)
    {
      m_Property = prop;
      m_Mobile = mobile;
      m_Object = o;
      m_Stack = stack;
      m_Type = type;
      m_Page = page;
      m_List = list;
    }

    protected override void OnTarget(Mobile from, object targeted)
    {
      try
      {
        if (m_Type == typeof(Type))
          targeted = targeted.GetType();
        else if ((m_Type == typeof(BaseAddon) || m_Type.IsAssignableFrom(typeof(BaseAddon))) &&
                 targeted is AddonComponent addonComponent)
          targeted = addonComponent.Addon;

        if (m_Type.IsInstanceOfType(targeted))
        {
          CommandLogging.LogChangeProperty(m_Mobile, m_Object, m_Property.Name, targeted.ToString());
          m_Property.SetValue(m_Object, targeted, null);
          PropertiesGump.OnValueChanged(m_Object, m_Property, m_Stack);
        }
        else
        {
          m_Mobile.SendMessage("That cannot be assigned to a property of type : {0}", m_Type.Name);
        }
      }
      catch
      {
        m_Mobile.SendMessage("An exception was caught. The property may not have changed.");
      }
    }

    protected override void OnTargetFinish(Mobile from)
    {
      if (m_Type == typeof(Type))
        from.SendGump(new PropertiesGump(m_Mobile, m_Object, m_Stack, m_List, m_Page));
      else
        from.SendGump(new SetObjectGump(m_Property, m_Mobile, m_Object, m_Stack, m_Type, m_Page, m_List));
    }
  }
}