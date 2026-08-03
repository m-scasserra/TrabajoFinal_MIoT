import { createContext, useContext, useState, type ReactNode } from "react";

interface TopBarState {
  title: string;
  actions: ReactNode;
  setTopBar: (title: string, actions?: ReactNode) => void;
}

const TopBarContext = createContext<TopBarState | null>(null);

export function TopBarProvider({ children }: { children: ReactNode }) {
  const [title, setTitle] = useState("");
  const [actions, setActions] = useState<ReactNode>(null);

  const setTopBar = (t: string, a: ReactNode = null) => {
    setTitle(t);
    setActions(a);
  };

  return (
    <TopBarContext.Provider value={{ title, actions, setTopBar }}>
      {children}
    </TopBarContext.Provider>
  );
}

export function useTopBar() {
  const ctx = useContext(TopBarContext);
  if (!ctx) throw new Error("useTopBar must be used within a TopBarProvider");
  return ctx;
}

import { useEffect } from "react";
export function useSetTopBar(title: string, actions?: ReactNode) {
  const { setTopBar } = useTopBar();
  useEffect(() => {
    setTopBar(title, actions);
    return () => setTopBar("", null);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [title]);
}
