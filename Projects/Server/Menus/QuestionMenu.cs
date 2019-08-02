/***************************************************************************
 *                              QuestionMenu.cs
 *                            -------------------
 *   begin                : May 1, 2002
 *   copyright            : (C) The RunUO Software Team
 *   email                : info@runuo.com
 *
 *   $Id$
 *
 ***************************************************************************/

/***************************************************************************
 *
 *   This program is free software; you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation; either version 2 of the License, or
 *   (at your option) any later version.
 *
 ***************************************************************************/

using Server.Network;

namespace Server.Menus.Questions
{
  public class QuestionMenu : IMenu
  {
    private static int m_NextSerial;
    private int m_Serial;

    public QuestionMenu(string question, string[] answers)
    {
      Question = question;
      Answers = answers;

      do
      {
        m_Serial = ++m_NextSerial;
        m_Serial &= 0x7FFFFFFF;
      } while (m_Serial == 0);
    }

    public string Question{ get; set; }

    public string[] Answers{ get; }

    int IMenu.Serial => m_Serial;

    int IMenu.EntryLength => Answers.Length;

    public virtual void OnCancel(NetState state)
    {
    }

    public virtual void OnResponse(NetState state, int index)
    {
    }

    public void SendTo(NetState state)
    {
      state.AddMenu(this);
      state.Send(new DisplayQuestionMenu(this));
    }
  }
}