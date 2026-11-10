import React from 'react';

export const PaddockLogo: React.FC<{
  size?: 'sm' | 'lg';
  onClick?: () => void;
}> = ({ size = 'sm', onClick }) => {
  return (
    <div
      className={`paddock-logo paddock-logo-${size}`}
      onClick={onClick}
      role={onClick ? 'button' : undefined}
      tabIndex={onClick ? 0 : undefined}
      title={onClick ? 'Voltar para a seleção inicial de categorias' : undefined}
    >
      <img src="/paddock.png" alt="Paddock Space" className="paddock-logo-img" />
    </div>
  );
};

