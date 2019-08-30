﻿using System;
using System.Collections.Generic;
using System.Text;
using Caliburn.Micro;
using ImageMagitek.Project;
using Monaco.PathTree;

namespace TileShop.WPF.ViewModels
{
    public class ProjectTreeArrangerViewModel : Screen
    {
        public IPathTreeNode<IProjectResource> Node { get; set; }

        public string Name => Node.Name;

        public ProjectTreeArrangerViewModel(IPathTreeNode<IProjectResource> node)
        {
            Node = node;
        }
    }
}